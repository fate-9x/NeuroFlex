using UnityEngine;

public static class ConfirmInput
{
    private static int _confirmFrame  = -1;
    private static int _skipFrame     = -1;

    public static void RaiseConfirm()      => _confirmFrame = Time.frameCount;
    public static void RaiseSkipSubtitle() => _skipFrame    = Time.frameCount;

    public static bool ConfirmWasPressedThisFrame() =>
        InputManager.Confirm.WasPressedThisFrame() || _confirmFrame == Time.frameCount;

    public static bool SkipSubtitleWasPressedThisFrame() =>
        InputManager.SkipSubtitle.WasPressedThisFrame() || _skipFrame == Time.frameCount;
}
