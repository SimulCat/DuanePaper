using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)] // Keeps performance up

public class UdonQuantized : UdonSharpBehaviour
{
    [Header("Lattice Diagram")]
    [SerializeField]
    UdonGrid2D theLattice;

    [SerializeField] SyncedSlider rotationSlider;

    [SerializeField, FieldChangeCallback(nameof(LatticeRotation))]
    private float latticeRotation;
    [Header("Reciprocal Diagram")]
    [SerializeField] UdonReciprocal2D theReciprocal;

    [SerializeField]
    LaserVectorLine IncidentRay;
    [SerializeField]
    LaserVectorLine ExitRay;

    [Header("Scale")]
    [SerializeField, Range(0.01f, 0.5f)] private float angstromsToGridScale = 0.04f;
    //[SerializeField, Range(0.01f, 0.5f)] private float momentumToGridScale = 0.075f;
    [SerializeField] private float lineLength = 0.77f;

    [Header("UI Controls")]

    [SerializeField] SyncedSlider AngstromSliderA;

    [SerializeField,FieldChangeCallback(nameof(PitchAngstromsA))] 
    float pitchAngstromsA = 4.5f;
    float PitchAngstromsA
    {
        get => pitchAngstromsA;
        set
        {
            if (pitchAngstromsA != value)
            {
                RefreshGrids();
            }
            pitchAngstromsA = value;
            RequestSerialization();
        }
    }
    [SerializeField] SyncedSlider AngstromSliderB;

    [SerializeField, FieldChangeCallback(nameof(PitchAngstromsB))]
    float pitchAngstromsB = 3f;
    float PitchAngstromsB
    {
        get => pitchAngstromsB; 
        set
        {
            if (pitchAngstromsB != value)
            {
                RefreshGrids();
            }
            pitchAngstromsB = value;
        }
    }


    [SerializeField] SyncedSlider IncidentControl;
    [SerializeField, FieldChangeCallback(nameof(IncidentTheta))]
    public float incidentTheta;
    private float IncidentTheta
    {
        get => incidentTheta;
        set
        {
            if (isInitialized)
            {
                incidentTheta = value;
                if (IncidentRay != null)
                {
                    IncidentRay.ThetaDegrees = incidentTheta;
                    CalcDelta();
                }
            }
        }
    }

    [SerializeField] SyncedSlider ExitControl;
    [SerializeField,FieldChangeCallback(nameof(ExitTheta))]
    public float exitTheta;
    public float ExitTheta
    {
        get => exitTheta;
        set
        {
            exitTheta = value;
            if (ExitRay != null)
            {
                ExitRay.SetProgramVariable("thetaDegrees",exitTheta);
                CalcDelta();
            }
        }
    }
    [SerializeField] SyncedSlider beamImpulseSlider;


    [SerializeField,FieldChangeCallback(nameof(BeamImpulse))]
    private float beamImpulse = 16;
    public float BeamImpulse
    {
        get => beamImpulse; 
        set
        {
            beamImpulse = value;
            CalcDelta();
            if (theReciprocal != null)
            {
                theReciprocal.BeamImpulse = value;
            }
        }
    }

    [SerializeField] private float maxBeamImpulse = 20;
    [SerializeField] private float minBeamImpulse = 5;
    [Header("Actually Constants")]

//    [SerializeField] float AngstromToP24 = 6.62607004f; // Converts 1/d (Angstroms) to units of momentum e-24.
//    [SerializeField] float photonKeVtoP24 = 0.534429723f;     // Converts KeV to units of momentum e-24.

    [Header("Calculated Values (Debug)")]

    private Transform latticeTransform;
    public float LatticeRotation
    {
        get => latticeRotation;
        set
        {
            if (latticeRotation != value)
            {
                latticeRotation = value;
                if (latticeTransform != null)
                    latticeTransform.localRotation = Quaternion.Euler(0, 0, latticeRotation);
                if (theReciprocal != null)
                    theReciprocal.LatticeRotation = value;
                CalcDelta();
            }
        }
    }

    private bool isInitialized = false;

    private void CalcDelta()
    {
        IncidentRay.LineLength = lineLength;
        ExitRay.LineLength = lineLength;

        if (theReciprocal != null)
            theReciprocal.BeamAnglesDeg = new Vector2(incidentTheta, exitTheta);
    }
    private void RefreshGrids()
    {
        Vector2 gridSpacing = new Vector2(pitchAngstromsA, pitchAngstromsB);
        if (theReciprocal != null)
        {
            theReciprocal.LatticeAngstroms = gridSpacing;
        }
        gridSpacing *= angstromsToGridScale;
        if (theLattice != null)
        {
            theLattice.GridSpacing = gridSpacing;
        }
    }

    private void Update()
    {
        if (isInitialized)
            return;
        CalcDelta();
        isInitialized = true;
    }

    private bool isEnabled = false;
    void OnEnable()
    {
        if (isEnabled)
            return;
        isEnabled = true;
        if (rotationSlider != null)
            rotationSlider.SetValues(latticeRotation, -90f, 90f);
        if (AngstromSliderA != null)
            AngstromSliderA.SetValues(pitchAngstromsA, 2f, 4.5f);
        if (AngstromSliderB != null)
            AngstromSliderB.SetValues(pitchAngstromsB, 2f, 4.5f);
        if (IncidentControl != null)
            IncidentControl.SetValues(incidentTheta, -67.5f, 67.5f);
        if (ExitControl != null)
            ExitControl.SetValues(exitTheta, -67.5f, 67.5f);
        if (beamImpulseSlider != null)
            beamImpulseSlider.SetValues(beamImpulse, minBeamImpulse, maxBeamImpulse);
        if (IncidentRay != null)
        {
            IncidentRay.ThetaDegrees = incidentTheta;
            IncidentRay.LineLength = lineLength;
        }
        if (ExitRay != null)
        {
            ExitRay.ThetaDegrees = exitTheta;
            ExitRay.LineLength = lineLength;
        }
        if (theLattice != null)
            latticeTransform = theLattice.transform;
        if (theReciprocal != null)
        {
            theReciprocal.MinBeamImpulse = minBeamImpulse;
            theReciprocal.MaxBeamImpulse = maxBeamImpulse;
            theReciprocal.BeamImpulse = beamImpulse;
            theReciprocal.BeamAnglesDeg = new Vector2(incidentTheta, exitTheta);
        }
    }
    void Start()
    {
        if (theReciprocal != null)
        {
            theReciprocal.MinBeamImpulse = minBeamImpulse;
            theReciprocal.MaxBeamImpulse = maxBeamImpulse;
            theReciprocal.BeamImpulse = beamImpulse;
            theReciprocal.BeamAnglesDeg = new Vector2(incidentTheta, exitTheta);
            //momentumToGridScale = theReciprocal.momentumToGridScale;
        }

        LatticeRotation = latticeRotation;
        PitchAngstromsA = pitchAngstromsA;
        PitchAngstromsB = pitchAngstromsB;
        IncidentTheta = incidentTheta;
        ExitTheta = exitTheta;
        BeamImpulse = beamImpulse;
        isInitialized = false;
    }

}
