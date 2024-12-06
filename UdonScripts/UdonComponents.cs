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
    [SerializeField] UdonBehaviour Incident;
    [SerializeField] UdonBehaviour Exit;
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
                    Incident.SetProgramVariable("thetaDegrees",incidentTheta);
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
                    Exit.SetProgramVariable("thetaDegrees",exitTheta);
                    CalcDelta();
                }
            }
        }
    }
    [SerializeField]
    SyncedSlider RotationControl;

    [SerializeField, FieldChangeCallback(nameof(LatticeRotation))]
    public float latticeRotation;


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

    void Start()
    {
        if (latticeGrid!= null)
        {
            latticeTransform = latticeGrid.transform;
        }
        if (Incident != null)
        {
            Incident.SetProgramVariable("thetaDegrees",incidentTheta);
            Incident.SetProgramVariable("lineLength",lineLength);
        }
        if (Exit != null)
        {
            Exit.SetProgramVariable("thetaDegrees",exitTheta);
            Exit.SetProgramVariable("lineLength", lineLength);
        }
        IncidentTheta = incidentTheta;
        if (IncidentControl != null)
            IncidentControl.SetValues(incidentTheta, -45, 45);
        ExitTheta = exitTheta;
        if (ExitControl != null)
            ExitControl.SetValues(exitTheta, -135, 135);
        LatticeRotation = latticeRotation;
        if (RotationControl != null)
            RotationControl.SetValues(0,-45,45);
        AlphaX = alphaX;
        if (togAlphaX != null)
            togAlphaX.setState(alphaX > 0);
        ComponentAlphaY = alphaY;
        if (togAlphaY != null)
            togAlphaY.setState(alphaY > 0);
        AlphaDelta = alphaDelta;
        if (togAlphaDelta != null)
            togAlphaDelta.setState(alphaDelta > 0);
        CalcDelta();
        isInitialized = true;
    }
}
