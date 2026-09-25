using System;
using System.Collections;

public interface IBackend
{
    IEnumerator GetCurrentTerms(Action<bool, APIManager.TermsData> onResult);
    IEnumerator AcceptTerms(string versionId, string contentHash, Action<bool, string> onResult);
    IEnumerator CreateSession(string acceptanceToken, Action<bool> onSuccess);
    IEnumerator GetSessionStatus(Action<string> onStatus);
    IEnumerator SendMetrics(string jsonData, Action<bool> onSuccess);
    string GetTermsText();
}
