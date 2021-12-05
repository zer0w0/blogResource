using UnityEngine;

[RequireComponent(typeof(Animator))]
public class IkHelper : MonoBehaviour
{
    public bool enableFeetIk = true;
    [Range(0, 2)] [SerializeField] private float heightFromGroundRaycast = 1.2f;
    [Range(0, 2)] [SerializeField] private float raycastDownDistance = 1.5f;
    [SerializeField] private LayerMask environmentLayer;
    [SerializeField] private float pelvisOffset = 0f;
    [Range(0, 1)] [SerializeField] private float pelvisUpAndDownSpeed = 0.28f;
    [Range(0, 1)] [SerializeField] private float feetToIkPositionSpeed = 0.5f;
    public string leftFootAnimCurveName = "LeftFootCurve";
    public string rightFootAnimCurveName = "RightFootCurve";
    public string leftFootAnimAngleOffsetName = "LeftFootAngleY";
    public string rightFootAnimAngleOffsetName = "RightFootAngleY";

    public bool useIkFeature = false;

    public bool showSolverDebug = true;

    private Animator _animator;

    private Vector3 _rightFootPosition, _leftFootPosition;
    private Vector3 _rightFootIkPosition, _leftFootIkPosition;
    private Quaternion _leftFootIkRotation, _rightFootIkRotation;
    private float _lastPelvisPositionY, _lastRightFootPositionY, _lastLeftFootPositionY;

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        if(!enableFeetIk) return;
        if(!_animator) return;
        
        AdjustFeetTarget(ref _rightFootPosition, HumanBodyBones.RightFoot);
        AdjustFeetTarget(ref _leftFootPosition, HumanBodyBones.LeftFoot);

        FootPositionSolver(_rightFootPosition, ref _rightFootIkPosition, ref _rightFootIkRotation, _animator.GetFloat(rightFootAnimAngleOffsetName));
        FootPositionSolver(_leftFootPosition, ref _leftFootIkPosition, ref _leftFootIkRotation, _animator.GetFloat(leftFootAnimAngleOffsetName));
    }
    
    private void OnAnimatorIK(int layerIndex)
    {
        if(!enableFeetIk) return;
        if(!_animator) return;
        
        MovePelvisHeight();
        
        _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, _animator.GetFloat(rightFootAnimCurveName));
        if (useIkFeature)
        {
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, _animator.GetFloat(rightFootAnimCurveName));
        }
        MoveFeetToIkPoint(AvatarIKGoal.RightFoot, _rightFootIkPosition, _rightFootIkRotation, ref _lastRightFootPositionY);
        
        _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, _animator.GetFloat(leftFootAnimCurveName));
        if (useIkFeature)
        {
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, _animator.GetFloat(leftFootAnimCurveName));
        }
        MoveFeetToIkPoint(AvatarIKGoal.LeftFoot, _leftFootIkPosition, _leftFootIkRotation, ref _lastLeftFootPositionY);
    }

    void MoveFeetToIkPoint(AvatarIKGoal foot, Vector3 positionIkHolder, Quaternion rotationIkHolder,
        ref float lastFootPositionY)
    {
        Vector3 targetIkPosition = _animator.GetIKPosition(foot);

        if (positionIkHolder != Vector3.zero)
        {
            targetIkPosition = transform.InverseTransformPoint(targetIkPosition);
            positionIkHolder = transform.InverseTransformPoint(positionIkHolder);

            float yVar = Mathf.Lerp(lastFootPositionY, positionIkHolder.y, feetToIkPositionSpeed);
            targetIkPosition.y += yVar;

            lastFootPositionY = yVar;
            targetIkPosition = transform.TransformPoint(targetIkPosition);
            
            _animator.SetIKRotation(foot, rotationIkHolder);
        }
        _animator.SetIKPosition(foot, targetIkPosition);
    }

    void MovePelvisHeight()
    {
        if (_rightFootIkPosition == Vector3.zero || _leftFootIkPosition == Vector3.zero || _lastPelvisPositionY == 0f)
        {
            _lastPelvisPositionY = _animator.bodyPosition.y;
            return;
        }

        float lOffsetPosition = _leftFootIkPosition.y - transform.position.y;
        float rOffsetPosition = _rightFootIkPosition.y - transform.position.y;

        float totalOffset = (lOffsetPosition < rOffsetPosition) ? lOffsetPosition : rOffsetPosition;

        Vector3 newPelvisPosition = _animator.bodyPosition + Vector3.up * totalOffset;

        newPelvisPosition.y = Mathf.Lerp(_lastPelvisPositionY, newPelvisPosition.y, pelvisUpAndDownSpeed);

        _animator.bodyPosition = newPelvisPosition;

        _lastPelvisPositionY = _animator.bodyPosition.y;
    }

    void FootPositionSolver(Vector3 fromSkyPosition, ref Vector3 feetIkPosition, ref Quaternion feetIkRotation, float angleOffset)
    {
        if(showSolverDebug)
            Debug.DrawLine(fromSkyPosition, fromSkyPosition + Vector3.down*(raycastDownDistance+heightFromGroundRaycast), Color.green);

        if (Physics.Raycast(fromSkyPosition, Vector3.down, out var feetOutHit,
            raycastDownDistance + heightFromGroundRaycast, environmentLayer))
        {
            feetIkPosition = fromSkyPosition;
            feetIkPosition.y = feetOutHit.point.y + pelvisOffset;
            
            feetIkRotation = Quaternion.FromToRotation(Vector3.up, feetOutHit.normal) * transform.rotation;
            feetIkRotation = Quaternion.AngleAxis(angleOffset, Vector3.up) * feetIkRotation;
            
            if(showSolverDebug)
                Debug.DrawRay(feetOutHit.point, feetOutHit.normal, Color.red);
            
            return;
        }

        feetIkPosition = Vector3.zero;
    }

    void AdjustFeetTarget(ref Vector3 feetPosition, HumanBodyBones foot)
    {
        feetPosition = _animator.GetBoneTransform(foot).position;
        feetPosition.y = transform.position.y + heightFromGroundRaycast;
    }
    
}
