using UnityEngine;

public class TreeRandomizer : MonoBehaviour
{
    #region Component Assignments
    [Header("Component Assignments")]
    [SerializeField] [Tooltip("The material used for the trunk (Bark).")]
    private Material barkMaterial;
    [SerializeField] [Tooltip("The material used for the leaves.")]
    private Material leafMaterial;

    [Space(5)]
    [SerializeField] [Tooltip("Drag the Trunk cylinder here.")]
    private Renderer trunkRenderer;
    [SerializeField] [Tooltip("Drag the Leaves sphere here.")]
    private Renderer leafRenderer;
    #endregion

    #region Global Controls
    [Header("Global Controls")]
    [SerializeField] [Tooltip("Master toggle for all randomization features.")]
    private bool useRandomization = true;
    [SerializeField] [Tooltip("Randomizes the scale of the ENTIRE prefab.")]
    private bool randomizePrefabScale = true;
    [SerializeField] [Tooltip("Randomizes both trunk and leaf colors.")]
    private bool randomizeColor = true;
    [SerializeField] [Tooltip("Randomizes the Y-axis rotation.")]
    private bool randomizeRotation = true;
    #endregion

    #region Randomization Ranges
    [Header("Scale Ranges")]
    [SerializeField] [Tooltip("Minimum scale for the entire tree.")]
    private float minScale = 0.8f;
    [SerializeField] [Tooltip("Maximum scale for the entire tree.")]
    private float maxScale = 1.4f;

    [Header("Trunk Color Ranges")]
    [SerializeField] private Color trunkColorA = new Color(0.35f, 0.25f, 0.15f);
    [SerializeField] private Color trunkColorB = new Color(0.25f, 0.2f, 0.15f);

    [Header("Leaf Color Ranges")]
    [SerializeField] private Color leafColorA = new Color(0.3f, 0.5f, 0.2f);
    [SerializeField] private Color leafColorB = new Color(0.1f, 0.3f, 0.1f);
    #endregion

    #region Wind Settings
    [Header("Wind Animation")]
    [SerializeField] [Tooltip("Enables the unified swaying motion.")]
    private bool enableWind = true;
    [SerializeField] [Tooltip("Speed of the sway cycle.")]
    private float windSpeed = 1.0f;
    [SerializeField] [Tooltip("Degrees of sway intensity.")]
    private float swayIntensity = 1.5f;

    private float randomOffset;
    private Quaternion initialRootRotation;
    #endregion

    void Start()
    {
        // Capture initial rotation of the root and set a unique offset
        randomOffset = Random.Range(0f, 10f);
        initialRootRotation = transform.localRotation;

        ApplyBaseMaterials();

        if (useRandomization)
        {
            ExecuteRandomization();
        }
    }

    void Update()
    {
        // Handle unified sway by rotating the Root object
        if (enableWind)
        {
            ApplyUnifiedSway();
        }
    }

    #region Core Logic
    private void ApplyBaseMaterials()
    {
        // Assign bark and leaf materials to their respective renderers
        if (trunkRenderer != null && barkMaterial != null) trunkRenderer.material = barkMaterial;
        if (leafRenderer != null && leafMaterial != null) leafRenderer.material = leafMaterial;
    }

    private void ExecuteRandomization()
    {
        // 1. Prefab Scale: Scales the root, which scales everything inside it perfectly
        if (randomizePrefabScale)
        {
            float s = Random.Range(minScale, maxScale);
            transform.localScale = Vector3.one * s;
        }

        // 2. Color Logic: Lerps between A and B for both trunk and leaves
        if (randomizeColor)
        {
            if (trunkRenderer != null) trunkRenderer.material.color = Color.Lerp(trunkColorA, trunkColorB, Random.value);
            if (leafRenderer != null) leafRenderer.material.color = Color.Lerp(leafColorA, leafColorB, Random.value);
        }

        // 3. Rotation Logic: Randomize the Y rotation
        if (randomizeRotation)
        {
            transform.Rotate(0, Random.Range(0f, 360f), 0);
        }
    }

    private void ApplyUnifiedSway()
    {
        float sway = Mathf.Sin(Time.time * windSpeed + randomOffset) * swayIntensity;
        // Apply rotation to the entire prefab root
        transform.localRotation = initialRootRotation * Quaternion.Euler(sway, 0, 0);
    }
    #endregion
}

/* ================================================================================
DETAILED INSTRUCTIONS FOR IMPLEMENTATION:
================================================================================
1. UPDATE SCRIPT: Replace your 'TreeRandomizer.cs' code with the block above.
2. ALIGNMENT: 
   - Manually position the 'Trunk' and 'Leaves' so they look perfect. 
   - Use the 'V' key to vertex snap the bottom of the Sphere to the top of the Cylinder.
3. INSPECTOR ASSIGNMENTS:
   - Bark/Leaf Materials: Drag your materials into the script slots.
   - Renderers: Drag 'Trunk' and 'Leaves' from Hierarchy into the Renderer slots.
4. SLIPPERY PHYSICS:
   - Ensure you have a Physic Material (0 friction, Friction Combine: Minimum).
   - Assign it to the Colliders on BOTH the Trunk and Leaves.
5. PREFAB SAVE: Drag the 'Tree_Master_Root' into your Assets folder.
6. RESULT: The script now scales the ENTIRE object. This prevents the leaves 
   from "floating" because the gap between them scales proportionally.
*/