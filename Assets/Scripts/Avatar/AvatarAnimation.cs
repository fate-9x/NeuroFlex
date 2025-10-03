using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarAnimation : MonoBehaviour
{
    public Animator avatarAnimator; // El Animator del Avatar
    public CapsuleCollider playerCollider; // El CapsuleCollider del Player
    public float centerThreshold = 0.2f; // Ajusta este valor según necesites
    public float idleDelay = 2f; // Tiempo en segundos antes de volver a Idle

    private CapsuleCollider avatarCollider;
    private bool isInsidePlayerCollider = false;
    private bool isNearCenter = false;
    private Coroutine idleCoroutine;

    private void Start()
    {
        avatarCollider = GetComponent<CapsuleCollider>();
        if (avatarCollider == null)
        {
            Debug.LogError("No se encontró CapsuleCollider en el Avatar");
        }

        if (playerCollider == null)
        {
            Debug.LogError("No se ha asignado el CapsuleCollider del Player");
        }

        if (avatarAnimator == null)
        {
            Debug.LogError("No se ha asignado el Animator del Avatar");
        }

        SetIdleAnimation();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == playerCollider)
        {
            isInsidePlayerCollider = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other == playerCollider && isInsidePlayerCollider)
        {
            CheckCenterProximity();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == playerCollider)
        {
            isInsidePlayerCollider = false;
            isNearCenter = false;
            StartIdleTimer();
        }
    }

    private void CheckCenterProximity()
    {
        Vector3 avatarCenter = avatarCollider.bounds.center;
        Vector3 closestPoint = playerCollider.ClosestPoint(avatarCenter);

        Vector3 relativeDistance = (closestPoint - playerCollider.bounds.center);
        relativeDistance.x /= playerCollider.radius;
        relativeDistance.y /= playerCollider.height / 2;
        relativeDistance.z /= playerCollider.radius;

        bool wasNearCenter = isNearCenter;
        isNearCenter = Mathf.Abs(relativeDistance.x) < centerThreshold &&
                       Mathf.Abs(relativeDistance.y) < centerThreshold &&
                       Mathf.Abs(relativeDistance.z) < centerThreshold;

        if (isNearCenter)
        {
            if (!wasNearCenter)
            {
                SetWalkAnimation();
            }
            if (idleCoroutine != null)
            {
                StopCoroutine(idleCoroutine);
                idleCoroutine = null;
            }
        }
        else if (wasNearCenter)
        {
            StartIdleTimer();
        }
    }

    private void StartIdleTimer()
    {
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
        }
        idleCoroutine = StartCoroutine(IdleTimerCoroutine());
    }

    private IEnumerator IdleTimerCoroutine()
    {
        yield return new WaitForSeconds(idleDelay);
        SetIdleAnimation();
    }

    private void SetIdleAnimation()
    {
        avatarAnimator.SetBool("IsIdle", true);
        avatarAnimator.SetBool("IsWalking", false);
        Debug.Log("Avatar en estado Idle.");
    }

    private void SetWalkAnimation()
    {
        avatarAnimator.SetBool("IsIdle", false);
        avatarAnimator.SetBool("IsWalking", true);
        Debug.Log("Avatar en estado Walk.");
    }

    private void OnDrawGizmos()
    {
        if (playerCollider != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerCollider.bounds.center, playerCollider.radius * centerThreshold);

            Gizmos.color = Color.green;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(playerCollider.bounds.center, playerCollider.transform.rotation, playerCollider.transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(playerCollider.radius * 2, playerCollider.height, playerCollider.radius * 2));
        }
    }
}
