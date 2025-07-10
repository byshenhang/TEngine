using UnityEngine;

namespace MikeNspired.VRConsoleLog
{
    public class MoveToObject : MonoBehaviour
    {
        [SerializeField] private Transform transformToMove;
        [SerializeField] private Transform targetTransform;
        [SerializeField] private Transform cameraTransform; 

        [SerializeField, Tooltip("Select the reference for alignment")] private AlignmentReference alignmentReference = AlignmentReference.TargetTransform;
        [SerializeField, Tooltip("Local offset to position object relative to target transform")] private Vector3 localOffset = new(0, 0, 1);
        [SerializeField, Tooltip("Align object's forward with the selected reference's forward or reverse")] private bool alignDirection;
        [SerializeField, Tooltip("Direction to align object's forward if 'alignDirection' is true")] private FacingDirection facingDirection = FacingDirection.Backward;
        [SerializeField, Tooltip("Make the object a child of the target transform")] private bool makeChildOfTarget;

        private void Awake()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            if (transformToMove == null) transformToMove = transform;
            if (cameraTransform == null) cameraTransform = Camera.main?.transform; // Default to main camera
        }

        public void Activate()
        {
            Transform referenceTransform = DetermineReferenceTransform();
            
            if (referenceTransform == null)
            {
                Debug.LogError("Reference transform is not assigned on " + gameObject.name);
                return;
            }

            transformToMove.position = referenceTransform.TransformPoint(localOffset);

            if (alignDirection)
            {
                transformToMove.forward = facingDirection == FacingDirection.Forward ? referenceTransform.forward : -referenceTransform.forward;
            }

            if (makeChildOfTarget) transformToMove.SetParent(referenceTransform, true);
        }

        private Transform DetermineReferenceTransform()
        {
            switch (alignmentReference)
            {
                case AlignmentReference.Camera:
                    return cameraTransform;
                case AlignmentReference.TargetTransform:
                default:
                    return targetTransform;
            }
        }

        private enum FacingDirection
        {
            Forward,  // Facing the same direction as the reference transform
            Backward  // Facing towards the reference transform (opposite to the reference's forward)
        }

        private enum AlignmentReference
        {
            TargetTransform, // Use targetTransform for positioning and alignment
            Camera     // Use cameraTransform for alignment
        }
    }
}


