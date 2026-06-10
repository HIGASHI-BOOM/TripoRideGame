using UnityEngine;

public sealed class KartScriptedInputSource : MonoBehaviour, IKartInputSource
{
    [SerializeField] private KartInputFrame currentInput;

    public KartInputFrame CurrentInput => currentInput.Sanitized();

    public void SetInput(KartInputFrame inputFrame)
    {
        currentInput = inputFrame.Sanitized();
    }
}
