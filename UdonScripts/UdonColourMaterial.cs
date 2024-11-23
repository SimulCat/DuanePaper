
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[RequireComponent(typeof(MeshRenderer))]
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class UdonColourMaterial : UdonSharpBehaviour
{
    [SerializeField]
    private MeshRenderer mr;
    [SerializeField, FieldChangeCallback(nameof(IdleColour))]
    private  Color idleColour = Color.gray;

    [SerializeField, FieldChangeCallback(nameof(ColourLevel))] private float colourLevel = 0f;
    [SerializeField]
    private Material mat;
    public float ColourLevel
    {
        get => colourLevel;
        set 
        {
            colourLevel = Mathf.Clamp01(value);
            if (mat != null)
            {
                mat.color = Color.Lerp(idleColour,highLightColour,colourLevel);
            }
        }
    }
    public Color IdleColour
    {
        get => idleColour;
        set
        {
            idleColour = value;
            ColourLevel = colourLevel;
        }
    }
    [SerializeField, FieldChangeCallback(nameof(HighLightColour))] 
    private Color highLightColour = Color.cyan;
    public Color HighLightColour
    {
        get => highLightColour;
        set 
        {
            highLightColour = value;
            ColourLevel = colourLevel;
        }
    }

    void Start()
    {
        if (mr == null)
            mr = GetComponent<MeshRenderer>();
        if (mat == null)
            mat = mr.material;
        if (mat != null)
        {
            idleColour = mat.color;
        }
        ColourLevel = colourLevel;
    }
}
