// using System;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
// using UnityEngine.Events;
// using UnityEngine.InputSystem;
//
// namespace MikeNspired.VRConsoleLog
// {
//     public class ButtonSequenceDetector : MonoBehaviour
//     {
//         [Tooltip("Initial state of the sequence detector.")]
//         [SerializeField] private State startingState = State.Deactivated;
//         [Tooltip("Triggers UnityEvents for starting state if true")]
//         [SerializeField] private bool callStartingStateEventOnStart;
//
//         public UnityEvent onActivated;
//         public UnityEvent onDeactivated;
//         public UnityEvent sequenceCompleted;
//
//         [Tooltip("Input that must be held down for the activation sequence to be considered.")]
//         [SerializeField]
//         private ActivationInput holdInput;
//
//         [Tooltip("List of inputs in the sequence to activate the console. You can use either InputAction or InputActionReference.")]
//         [SerializeField]
//         private List<ActivationInput> activationSequence;
//
//         private int currentIndex;
//         private bool isHoldInputActive;
//         private State currentState;
//
//         private void Awake() => currentState = startingState;
//         private void Start()
//         {
//             if (!callStartingStateEventOnStart) return;
//             if(startingState == State.Activated)
//                 onActivated?.Invoke();
//             else
//                 onDeactivated?.Invoke();
//         }
//
//         private void OnEnable() => SubscribeToInputActions();
//
//         private void OnDisable() => UnsubscribeFromInputActions();
//
//         private void SubscribeToInputActions()
//         {
//             var holdAction = holdInput.action;
//             if (holdAction != null)
//             {
//                 holdAction.Enable();
//                 holdAction.started += HandleHoldInputStarted;
//                 holdAction.canceled += HandleHoldInputCanceled;
//             }
//
//             // Get distinct actions to prevent multiple subscriptions
//             var comparer = new InputActionComparer();
//             var distinctActions = activationSequence.Select(ai => ai.action)
//                 .Where(action => action != null)
//                 .Distinct(comparer);
//
//             // Subscribe to all distinct actions in the activation sequence
//             foreach (var action in distinctActions)
//             {
//                 action.Enable();
//                 action.performed += HandleActionPerformed;
//             }
//         }
//
//         private void UnsubscribeFromInputActions()
//         {
//             var holdAction = holdInput.action;
//             if (holdAction != null)
//             {
//                 holdAction.started -= HandleHoldInputStarted;
//                 holdAction.canceled -= HandleHoldInputCanceled;
//             }
//             
//             var comparer = new InputActionComparer();
//             var distinctActions = activationSequence.Select(activationInput => activationInput.action)
//                 .Where(action => action != null)
//                 .Distinct(comparer);
//
//             // Unsubscribe from all actions in the activation sequence
//             foreach (var action in distinctActions)
//             {
//                 action.performed -= HandleActionPerformed;
//             }
//         }
//
//         private void HandleActionPerformed(InputAction.CallbackContext context)
//         {
//               // If the hold input is not active, return early
//             if (!isHoldInputActive) return;
//
//             var expectedAction = activationSequence[currentIndex].action;
//             if (currentIndex < activationSequence.Count && context.action.bindings.Any(b => b.path == expectedAction.bindings[0].path))
//             {
//                 currentIndex++;
//                 if (currentIndex == activationSequence.Count)
//                 {
//                     sequenceCompleted.Invoke();
//                     ToggleState();
//                     currentIndex = 0; // Reset the index to allow for reactivation
//                 }
//             }
//             else
//             {
//                 currentIndex = 0; // Reset the index if the sequence is broken
//             }
//         }
//
//         private void HandleHoldInputStarted(InputAction.CallbackContext context) => isHoldInputActive = true;
//
//         private void HandleHoldInputCanceled(InputAction.CallbackContext context)
//         {
//             isHoldInputActive = false;
//             currentIndex = 0; // Optionally reset the sequence if the hold input is released before completion
//         }
//
//         private void ToggleState()
//         {
//             if (currentState == State.Activated)
//             {
//                 currentState = State.Deactivated;
//                 onDeactivated.Invoke();
//             }
//             else
//             {
//                 currentState = State.Activated;
//                 onActivated.Invoke();
//             }
//         }
//
//         [Serializable]
//         private struct ActivationInput
//         {
//             public InputAction inputAction;
//             public InputActionReference inputActionReference;
//
//             public InputAction action => inputAction ?? (inputActionReference != null ? inputActionReference.action : null);
//         }
//         
//         private class InputActionComparer : IEqualityComparer<InputAction>
//         {
//             public bool Equals(InputAction x, InputAction y)
//             {
//                 if (ReferenceEquals(x, y)) return true;
//                 if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
//                     return false;
//
//                 // Comparing based on the first binding path
//                 return x.bindings.Count > 0 && y.bindings.Count > 0 &&
//                        x.bindings[0].path == y.bindings[0].path;
//             }
//
//             public int GetHashCode(InputAction obj)
//             {
//                 // Use the hash code of the first binding path
//                 return obj.bindings.Count > 0 ? obj.bindings[0].path.GetHashCode() : 0;
//             }
//         }
//         
//         private enum State
//         {
//             Activated,
//             Deactivated
//         }
//     }
// }

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace MikeNspired.VRConsoleLog
{
    public class ButtonSequenceDetector : MonoBehaviour
    {
        [Tooltip("Initial state of the sequence detector.")] [SerializeField]
        private State startingState = State.Deactivated;

        [Tooltip("Triggers UnityEvents for starting state if true")] [SerializeField]
        private bool callStartingStateEventOnStart;

        public UnityEvent onActivated;
        public UnityEvent onDeactivated;
        public UnityEvent sequenceCompleted;

        [Header("Defaults to String identifier if specified"),Space]
        
        [Tooltip("Key that must be held down for the activation sequence to be considered.")] [SerializeField]
        private KeyAction holdKey;

        [Tooltip("Sequence of actions to activate the console.")] [SerializeField]
        private List<KeyAction> activationSequence;

        private int currentIndex;
        private bool isHoldInputActive;

        private void Start()
        {
            if (!callStartingStateEventOnStart) return;
            if (startingState == State.Activated)
                onActivated?.Invoke();
            else
                onDeactivated?.Invoke();
        }

        private void Update()
        {
            HandleHoldInput();
            HandleSequenceInput();
        }

        private void HandleHoldInput()
        {
            if (Input.GetKey(holdKey.GetEffectiveKeyCode()))
                isHoldInputActive = true;
            else if (Input.GetKeyUp(holdKey.GetEffectiveKeyCode()))
            {
                isHoldInputActive = false;
                currentIndex = 0; // Optionally reset the sequence if the hold input is released before completion
            }
        }

        private void HandleSequenceInput()
        {
            if (!isHoldInputActive) return;

            if (currentIndex < activationSequence.Count &&
                Input.GetKeyDown(activationSequence[currentIndex].GetEffectiveKeyCode()))
            {
                currentIndex++;
                if (currentIndex == activationSequence.Count)
                {
                    sequenceCompleted.Invoke();
                    ToggleState();
                    currentIndex = 0; // Reset the index to allow for reactivation
                }
            }
            else if (currentIndex < activationSequence.Count && Input.anyKeyDown)
            {
                currentIndex = 0; // Reset the index if the sequence is broken
            }
        }

        private void ToggleState()
        {
            if (startingState == State.Activated)
            {
                startingState = State.Deactivated;
                onDeactivated.Invoke();
            }
            else
            {
                startingState = State.Activated;
                onActivated.Invoke();
            }
        }

        private enum State
        {
            Activated,
            Deactivated
        }

        // Define the KeyAction struct
        [System.Serializable]
        public struct KeyAction
        {
            public KeyCode key;
            public string stringIdentifier;

            public KeyCode GetEffectiveKeyCode()
            {
                if (!string.IsNullOrEmpty(stringIdentifier))
                {
                    // Attempt to parse the identifier into a KeyCode
                    if (System.Enum.TryParse(stringIdentifier, out KeyCode result))
                        return result;
                }

                return key;
            }
        }
    }
}