using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Oculus.Interaction;

public class TermsManager : MonoBehaviour
{
    [SerializeField] private TMP_Text termsTitleText;
    [SerializeField] private TMP_Text termsContentText;
    [SerializeField] private GameObject aceptarButton;
    [SerializeField] private GameObject rechazarButton;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private SceneController sceneController;

    private APIManager apiManager;
    private string versionId;
    private string contentHash;
    private bool scrollGatePassed;
    private bool isAccepting;
    private Button aceptarBtnComponent;

    private void Start()
    {
        GameObject utilsObj = GameObject.Find("Utils");
        if (utilsObj != null)
        {
            apiManager = utilsObj.GetComponent<APIManager>();
        }

        if (apiManager == null)
        {
            SetError("No se encontró APIManager en Utils.");
            return;
        }

        if (aceptarButton != null)
        {
            aceptarBtnComponent = aceptarButton.GetComponent<Button>();
            if (aceptarBtnComponent != null)
            {
                aceptarBtnComponent.onClick.AddListener(AceptarOnClick);
            }
            else
            {
                var wrapper = aceptarButton.GetComponent<InteractableUnityEventWrapper>();
                if (wrapper != null)
                {
                    wrapper.WhenSelect.AddListener(AceptarOnClick);
                }
            }
        }
        // rechazarButton ya wired a QuitApplication en la escena; no sobreescribir.
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(OnScrollChanged);
        }
        if (errorText != null) errorText.text = "";
        if (termsTitleText != null) termsTitleText.text = "";
        if (termsContentText != null) termsContentText.text = "";

        SetButtonsVisible(false);
        StartCoroutine(FetchTermsWithRetry(0));
    }

    private IEnumerator FetchTermsWithRetry(int attempt)
    {
        bool completed = false;
        bool ok = false;
        APIManager.TermsData data = null;
        apiManager.GetCurrentTerms((success, d) =>
        {
            completed = true;
            ok = success;
            data = d;
        });

        while (!completed) yield return null;

        if (!ok || data == null)
        {
            if (attempt < 2)
            {
                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(FetchTermsWithRetry(attempt + 1));
                yield break;
            }
            SetError("Error al cargar términos. Reinicie la app.");
            yield break;
        }

        if (termsTitleText != null) termsTitleText.text = data.title;
        if (termsContentText != null) termsContentText.text = data.content;
        versionId = data.version_id;
        contentHash = data.content_hash;
        scrollGatePassed = false;
        SetButtonsVisible(true);
        if (aceptarBtnComponent != null) aceptarBtnComponent.interactable = false;
        // El scroll-gate se evalúa en el primer LateUpdate y vía onValueChanged.
        EvaluateScrollGate();
    }

    private void OnScrollChanged(Vector2 _)
    {
        EvaluateScrollGate();
    }

    private void EvaluateScrollGate()
    {
        if (aceptarButton == null || !aceptarButton.activeSelf) return;
        if (scrollGatePassed) return;

        bool atBottom;
        if (scrollRect == null || scrollRect.content == null || scrollRect.viewport == null)
        {
            atBottom = true;
        }
        else
        {
            float scrollableHeight = scrollRect.content.rect.height - scrollRect.viewport.rect.height;
            if (scrollableHeight <= 0.5f)
            {
                atBottom = true;
            }
            else
            {
                atBottom = scrollRect.normalizedPosition.y <= 0.0001f;
            }
        }

        if (atBottom)
        {
            scrollGatePassed = true;
            if (aceptarBtnComponent != null) aceptarBtnComponent.interactable = true;
            if (errorText != null && errorText.text.Contains("Deslice"))
            {
                errorText.text = "";
            }
        }
        else
        {
            if (aceptarBtnComponent != null) aceptarBtnComponent.interactable = false;
            if (errorText != null)
            {
                errorText.text = "Deslice hasta el final para aceptar";
            }
        }
    }

    private void LateUpdate()
    {
        if (scrollGatePassed) return;
        // Re-evalúa el scroll-gate en el primer frame tras renderizar el contenido,
        // para el edge case donde el contenido cabe sin scroll.
        if (termsContentText != null && !string.IsNullOrEmpty(termsContentText.text))
        {
            EvaluateScrollGate();
        }
    }

    public void AceptarOnClick()
    {
        if (!scrollGatePassed) return;
        if (isAccepting) return;
        isAccepting = true;
        if (apiManager == null || string.IsNullOrEmpty(versionId) || string.IsNullOrEmpty(contentHash))
        {
            SetError("Términos no cargados. Reinicie la app.");
            isAccepting = false;
            return;
        }
        if (aceptarBtnComponent != null) aceptarBtnComponent.interactable = false;
        if (errorText != null) errorText.text = "";
        StartCoroutine(AcceptTermsCoroutine());
    }

    private IEnumerator AcceptTermsCoroutine()
    {
        bool completed = false;
        bool ok = false;
        string token = null;
        apiManager.AcceptTerms(versionId, contentHash, (success, t) =>
        {
            completed = true;
            ok = success;
            token = t;
        });

        while (!completed) yield return null;

        if (!ok || string.IsNullOrEmpty(token))
        {
            SetError("Error al aceptar. Reintente.");
            if (aceptarBtnComponent != null) aceptarBtnComponent.interactable = true;
            isAccepting = false;
            yield break;
        }

        // AcceptanceToken ya guardado en APIManager por AcceptTerms.
        isAccepting = false;
        if (sceneController != null)
        {
            sceneController.LoadScene("CreateSession");
        }
        else
        {
            SetError("SceneController no asignado en TermsManager.");
        }
    }

    private void SetButtonsVisible(bool visible)
    {
        if (aceptarButton != null) aceptarButton.SetActive(visible);
        if (rechazarButton != null) rechazarButton.SetActive(visible);
    }

    private void SetError(string msg)
    {
        if (errorText != null) errorText.text = msg;
        Debug.LogWarning(msg);
    }
}
