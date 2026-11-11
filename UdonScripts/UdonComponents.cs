using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)] // Keeps performance up
public class UdonComponents : UdonSharpBehaviour
{
    [Header("Lattice Diagram")]
    [SerializeField]
    UdonBehaviour latticeGrid;

    [Header("Vector Pointers")]
    [SerializeField] LaserVectorLine Incident;
    [SerializeField] LaserVectorLine Exit;
    [SerializeField] LaserVectorLine laserVecDelta;
    [SerializeField] LaserVectorLine laserVecX;
    [SerializeField] LaserVectorLine laserVecY;
    [Header("Ewald Circle Prefab"), SerializeField]
    UdonBehaviour ewaldCircle;
    [Header("Control Settings")]

    [Range(0f, 1f),FieldChangeCallback(nameof(OverlayShift))]
    public float overlayShift = 0;
    public float OverlayShift 
    { 
        get => overlayShift;
        set
        {
            if (overlayShift != value)
            {
                overlayShift = value;
                UpdateOverlay();
            }
        }
    }

    [Header("UI Controls")]
    [SerializeField]
    SyncedSlider IncidentControl;
    bool isInitialized = false;
    [SerializeField, FieldChangeCallback(nameof(IncidentTheta))]
    public float incidentTheta;
    private float IncidentTheta
    {
        get => incidentTheta;
        set
        {
            incidentTheta = value;
            if (isInitialized)
            {
                if (Incident != null)
                {
                    Incident.ThetaDegrees = incidentTheta;
                    CalcDelta();
                }
                RequestSerialization();
            }
        }
    }

    [SerializeField]
    SyncedSlider ExitControl;
    [SerializeField, FieldChangeCallback(nameof(ExitTheta))]
    public float exitTheta;
    private float ExitTheta
    {
        get => exitTheta;
        set
        {
            exitTheta = value;
            if (isInitialized)
            {
                if (Exit != null)
                {
                    Exit.ThetaDegrees = exitTheta;
                    CalcDelta();
                }
            }
        }
    }
    [SerializeField]
    SyncedSlider RotationControl;

    [SerializeField, FieldChangeCallback(nameof(LatticeRotation))]
    public float latticeRotation;

    [SerializeField]
    private bool debug = false;
    private Transform latticeTransform;

    public float LatticeRotation { 
        get => latticeRotation; 
        set 
        {
            if (latticeRotation != value)
            {
                latticeRotation = value;
                if (latticeTransform!= null)
                {
                    latticeTransform.localRotation = Quaternion.Euler(0,0,latticeRotation);
                }
                CalcDelta();
            }
        } 
    }

    [SerializeField] private bool showEwald = true;

    [SerializeField] SyncedTween togAlphaX;
    [SerializeField, FieldChangeCallback(nameof(AlphaX))]
    float alphaX = 1f;
    float AlphaX 
    { 
        get => alphaX;
        set
        {
            alphaX = value;
            float v = Mathf.Clamp01(value);
            if (laserVecX != null)
                laserVecX.Alpha = v;
        }
    }
    [SerializeField] SyncedTween togAlphaY;
    [SerializeField, FieldChangeCallback(nameof(ComponentAlphaY))]
    float alphaY = 1f;
    float ComponentAlphaY
    {
        get => alphaY;
        set
        {
            alphaY = value;
            float v = Mathf.Clamp01(value);
            if (laserVecY != null)
                laserVecY.Alpha = v;
        }
    }

    [SerializeField] SyncedTween togAlphaDelta;
    [SerializeField, FieldChangeCallback(nameof(AlphaDelta))]
    float alphaDelta = 1f;
    float AlphaDelta
    {
        get => alphaDelta;
        set
        {
            alphaDelta = value;
            float v = Mathf.Clamp01(value);
            if (laserVecDelta != null)
                laserVecDelta.Alpha = laserVecDelta.LineLength > 0 ? v : 0;
        }
    }

    [SerializeField]
    Vector2 deltaVec = Vector2.up;
    [SerializeField] 
    Vector2 exitVec = Vector2.right;
    [SerializeField]
    Vector2 incidentVec = Vector2.left;
    [SerializeField]
    private float lineLength = 0.77f;

    private void UpdateOverlay()
    {
        Vector2 DeltaOrigin = Vector2.Lerp(Vector2.zero, exitVec, overlayShift);
        Vector2 IncidentOrigin = Vector2.Lerp(Vector2.zero, incidentVec, overlayShift);
        if (laserVecDelta != null)
            laserVecDelta.transform.localPosition= DeltaOrigin;
        if (Incident != null)
            Incident.transform.localPosition= IncidentOrigin;
    }
    private void CalcDelta()
    {
        if (IncidentControl != null)
            IncidentControl.ValueOffset = -latticeRotation;
        if (ExitControl != null)
            ExitControl.ValueOffset = -latticeRotation;
        float thetaI = incidentTheta * Mathf.Deg2Rad;
        float thetaE = exitTheta * Mathf.Deg2Rad;
        incidentVec = new Vector2(Mathf.Cos(thetaI), Mathf.Sin(thetaI)) * lineLength;
        exitVec = new Vector2(Mathf.Cos(thetaE), Mathf.Sin(thetaE)) * lineLength;
        deltaVec = incidentVec - exitVec;
        float deltaLen = deltaVec.magnitude;
        float deltaTheta = Mathf.Atan2(deltaVec.y, deltaVec.x) * Mathf.Rad2Deg;
        float thetaLattice = latticeRotation * Mathf.Deg2Rad;
        float cosTheta = Mathf.Cos(-thetaLattice);
        float sinTheta = Mathf.Sin(-thetaLattice);
        Vector2 deltaLattice = new Vector2(cosTheta*deltaVec.x - sinTheta*deltaVec.y,sinTheta*deltaVec.x+ cosTheta*deltaVec.y);
        if (laserVecDelta != null) 
        {
            laserVecDelta.LineLength = deltaLen;
            laserVecDelta.ThetaDegrees = deltaTheta;
            laserVecDelta.Alpha = deltaLen > 0 ? alphaDelta : 0f;
        }
        if (laserVecX != null)
        {
            laserVecX.ThetaDegrees = (deltaLattice.x >= 0 ? 0f : 180f) + latticeRotation;
            laserVecX.LineLength = Mathf.Abs(deltaLattice.x);
            laserVecX.Alpha = deltaLattice.x != 0 ? alphaX : 0f;
        }
        if (laserVecY != null)
        {
            laserVecY.ThetaDegrees = (deltaLattice.y >= 0 ? 90f : -90f) + latticeRotation;
            laserVecY.LineLength = Mathf.Abs(deltaLattice.y);
            laserVecY.Alpha = deltaLattice.y != 0 ? alphaY : 0f;
        }
        if (showEwald && ewaldCircle!=null)
        {
            ewaldCircle.SetProgramVariable("radius", lineLength);
        }

        UpdateOverlay();
    }

    private void OnEnable()
    {
        if (debug)
            Debug.Log($"UdonComponents OnEnable called");
        if (latticeGrid != null)
        {
            latticeTransform = latticeGrid.transform;
        }
        if (Incident != null)
        {
            Incident.ThetaDegrees = incidentTheta;
            Incident.LineLength = lineLength;
        }
        if (Exit != null)
        {
            Exit.ThetaDegrees = exitTheta;
            Exit.LineLength = lineLength;
        }
        if (IncidentControl != null)
            IncidentControl.SetValues(incidentTheta, -45, 45);
        if (ExitControl != null)
            ExitControl.SetValues(exitTheta, -135, 135);
        if (RotationControl != null)
            RotationControl.SetValues(0, -45, 45);
        if (togAlphaX != null)
            togAlphaX.SetState(alphaX > 0);
        if (togAlphaY != null)
            togAlphaY.SetState(alphaY > 0);
        if (togAlphaDelta != null)
            togAlphaDelta.SetState(alphaDelta > 0);
        CalcDelta();
    }
    void Start()
    {
        IncidentTheta = incidentTheta;
        ExitTheta = exitTheta;
        LatticeRotation = latticeRotation;
        AlphaX = alphaX;
        ComponentAlphaY = alphaY;
        AlphaDelta = alphaDelta;
        isInitialized = true;
    }
}
