using UnityEngine;
using UnityEngine.InputSystem;

public static class InputManager
{
    public static InputAction Confirm { get; private set; }
    public static InputAction SkipSubtitle { get; private set; }
    public static InputAction DebugSkipScene { get; private set; }

    static InputManager()
    {
        var asset = InputActionAsset.FromJson(k_JSON);
        var player = asset.FindActionMap("Player", throwIfNotFound: true);
        Confirm = player.FindAction("Confirm", throwIfNotFound: true);
        SkipSubtitle = player.FindAction("SkipSubtitle", throwIfNotFound: true);
        DebugSkipScene = player.FindAction("DebugSkipScene", throwIfNotFound: true);
        player.Enable();
    }

    private const string k_JSON = @"{
    ""name"": ""NeuroFlexInputs"",
    ""maps"": [
        {
            ""name"": ""Player"",
            ""id"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
            ""actions"": [
                {
                    ""name"": ""Confirm"",
                    ""type"": ""Button"",
                    ""id"": ""b1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""SkipSubtitle"",
                    ""type"": ""Button"",
                    ""id"": ""c1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""DebugSkipScene"",
                    ""type"": ""Button"",
                    ""id"": ""d1b2c3d4-e5f6-7890-abcd-ef1234567890"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""e1000000-0000-0000-0000-000000000001"",
                    ""path"": ""<XRController>{RightHand}/primaryButton"",
                    ""action"": ""Confirm"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e1000000-0000-0000-0000-000000000002"",
                    ""path"": ""<XRController>{LeftHand}/primaryButton"",
                    ""action"": ""Confirm"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e1000000-0000-0000-0000-000000000003"",
                    ""path"": ""<Keyboard>/space"",
                    ""action"": ""Confirm"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e1000000-0000-0000-0000-000000000004"",
                    ""path"": ""<XRController>{RightHand}/triggerButton"",
                    ""action"": ""SkipSubtitle"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e1000000-0000-0000-0000-000000000005"",
                    ""path"": ""<XRController>{LeftHand}/triggerButton"",
                    ""action"": ""SkipSubtitle"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e1000000-0000-0000-0000-000000000006"",
                    ""path"": ""<Keyboard>/space"",
                    ""action"": ""SkipSubtitle"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e1000000-0000-0000-0000-000000000007"",
                    ""path"": ""<Keyboard>/m"",
                    ""action"": ""DebugSkipScene"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}";
}
