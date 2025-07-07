using UnityEngine;
using System.Collections;

/// <summary>
/// WebGL-compatible background pattern with proper texture loading
/// </summary>
public class WebGLBackgroundPattern : MonoBehaviour
{
    [Header("Pattern Settings")]
    [SerializeField] private Texture2D patternTexture;
    [SerializeField] private Vector2 gameAreaSize = new Vector2(200f, 200f);
    [SerializeField] private Color patternColor = new Color(1f, 1f, 1f, 0.7f); // Even brighter default
    [SerializeField] private Vector2 tiling = new Vector2(10f, 10f);
    [SerializeField] private float brightness = 2.0f; // Higher brightness multiplier for WebGL
    
    [Header("WebGL Settings")]
    [SerializeField] private bool setupOnStart = true;
    [SerializeField] private bool waitForTextureLoad = false; // Set to false for immediate loading
    [SerializeField] private float textureLoadTimeout = 1f; // Reduced timeout
    [SerializeField] private bool useSimpleMaterial = true; // Use a simple material for better WebGL compatibility
    
    private GameObject backgroundQuad;
    private Material patternMaterial;
    private bool textureLoaded = false;
    
    private void Start()
    {
        if (setupOnStart)
        {
            // Always setup immediately for faster loading
            SetupBackgroundPattern();
        }
    }
    
    /// <summary>
    /// Set up background pattern with delay to ensure texture is loaded in WebGL
    /// </summary>
    private IEnumerator SetupBackgroundPatternWithDelay()
    {
        // Only use this if explicitly requested
        if (!waitForTextureLoad)
        {
            SetupBackgroundPattern();
            yield break;
        }
        
        float elapsedTime = 0f;
        
        // Wait for texture to be properly loaded
        while (!textureLoaded && elapsedTime < textureLoadTimeout)
        {
            if (patternTexture != null && patternTexture.isReadable)
            {
                textureLoaded = true;
                break;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Force texture to be readable in WebGL
        if (patternTexture != null && !patternTexture.isReadable)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            // In WebGL, we need to ensure the texture is properly loaded
            yield return new WaitForSeconds(0.1f);
            #endif
        }
        
        SetupBackgroundPattern();
    }
    
    /// <summary>
    /// Set up the background pattern
    /// </summary>
    public void SetupBackgroundPattern()
    {
        if (patternTexture == null)
        {
            Debug.LogWarning("WebGLBackgroundPattern: No pattern texture assigned!");
            return;
        }
        
        CreateBackgroundQuad();
    }
    
    /// <summary>
    /// Create a single quad with tiling texture
    /// </summary>
    private void CreateBackgroundQuad()
    {
        // Create the quad GameObject
        backgroundQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backgroundQuad.name = "WebGLBackgroundPatternQuad";
        backgroundQuad.transform.SetParent(transform);
        backgroundQuad.transform.position = new Vector3(0f, 0f, 100f);
        backgroundQuad.transform.localScale = new Vector3(gameAreaSize.x, gameAreaSize.y, 1f);
        
        // Remove the collider
        DestroyImmediate(backgroundQuad.GetComponent<Collider>());
        
        // Create material with WebGL-compatible shader
        if (useSimpleMaterial)
        {
            CreateSimpleMaterial();
        }
        else
        {
            CreateWebGLMaterial();
        }
        
        // Apply material to the quad
        MeshRenderer renderer = backgroundQuad.GetComponent<MeshRenderer>();
        renderer.material = patternMaterial;
        renderer.sortingOrder = -1000;
        
        // Force brightness update after material is applied
        ForceBrightness();
        
        Debug.Log($"Created WebGL background pattern quad with tiling: {tiling}");
    }
    
    /// <summary>
    /// Create a WebGL-compatible material
    /// </summary>
    private void CreateWebGLMaterial()
    {
        // Try different shaders in order of preference for WebGL
        Shader shader = null;
        
        // First try Standard shader (most reliable in WebGL)
        shader = Shader.Find("Standard");
        if (shader == null)
        {
            // Try Unlit/Texture (might not be available in WebGL)
            shader = Shader.Find("Unlit/Texture");
        }
        if (shader == null)
        {
            // Try Legacy/Diffuse (very reliable)
            shader = Shader.Find("Legacy Shaders/Diffuse");
        }
        if (shader == null)
        {
            // Last resort: Sprites/Default
            shader = Shader.Find("Sprites/Default");
        }
        
        if (shader == null)
        {
            Debug.LogError("No suitable shader found for WebGL background pattern!");
            return;
        }
        
        patternMaterial = new Material(shader);
        patternMaterial.name = "WebGLBackgroundPatternMaterial";
        
        // Set texture with error checking
        if (patternTexture != null)
        {
            patternMaterial.mainTexture = patternTexture;
            Debug.Log($"Applied texture to material: {patternTexture.name}");
            
            // Check if texture is set to repeat
            if (patternTexture.wrapMode != TextureWrapMode.Repeat)
            {
                Debug.LogWarning($"Texture {patternTexture.name} is not set to Repeat! Current wrap mode: {patternTexture.wrapMode}");
                Debug.LogWarning("Please set the texture's Wrap Mode to 'Repeat' in the import settings.");
            }
        }
        else
        {
            Debug.LogError("Pattern texture is null!");
        }
        
        // Set tiling - this should make the texture repeat
        patternMaterial.mainTextureScale = tiling;
        patternMaterial.mainTextureOffset = Vector2.zero; // Reset offset
        patternMaterial.color = patternColor;
        
        // Additional WebGL-specific settings
        #if UNITY_WEBGL && !UNITY_EDITOR
        if (shader.name.Contains("Standard"))
        {
            patternMaterial.SetFloat("_Glossiness", 0f);
            patternMaterial.SetFloat("_Metallic", 0f);
        }
        #endif
        
        Debug.Log($"Material created with shader: {shader.name}, tiling: {tiling}, texture wrap mode: {patternTexture?.wrapMode}");
    }
    
    /// <summary>
    /// Create a simple material that's guaranteed to work in WebGL
    /// </summary>
    private void CreateSimpleMaterial()
    {
        // Try to use Unlit shader first (no lighting)
        Shader shader = Shader.Find("Unlit/Texture");
        if (shader == null)
        {
            // Fallback to Standard but we'll make it bright
            shader = Shader.Find("Standard");
        }
        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/Diffuse");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }
        
        if (shader == null)
        {
            Debug.LogError("No shader found for simple material!");
            return;
        }
        
        patternMaterial = new Material(shader);
        patternMaterial.name = "SimpleWebGLMaterial";
        
        // Set texture
        if (patternTexture != null)
        {
            patternMaterial.mainTexture = patternTexture;
            Debug.Log($"Applied texture to simple material: {patternTexture.name}");
        }
        else
        {
            Debug.LogError("Pattern texture is null!");
        }
        
        // Set basic properties
        patternMaterial.mainTextureScale = tiling;
        patternMaterial.mainTextureOffset = Vector2.zero;
        
        // Make the material bright and unaffected by lighting
        Color brightColor = patternColor * brightness;
        
        if (shader.name.Contains("Unlit"))
        {
            // Unlit shader - no lighting, use color as is
            patternMaterial.color = brightColor;
        }
        else if (shader.name.Contains("Standard"))
        {
            // Standard shader - disable lighting and make it bright
            patternMaterial.SetFloat("_Glossiness", 0f);
            patternMaterial.SetFloat("_Metallic", 0f);
            patternMaterial.SetFloat("_Mode", 0f); // Opaque mode
            patternMaterial.SetFloat("_Smoothness", 0f);
            
            // Make it bright by using emission
            patternMaterial.EnableKeyword("_EMISSION");
            patternMaterial.SetColor("_EmissionColor", brightColor * 3f); // Very bright emission
            patternMaterial.SetColor("_Color", brightColor);
            
            // Additional WebGL-specific settings
            #if UNITY_WEBGL && !UNITY_EDITOR
            patternMaterial.SetFloat("_Glossiness", 0f);
            patternMaterial.SetFloat("_Metallic", 0f);
            patternMaterial.SetFloat("_Smoothness", 0f);
            patternMaterial.SetFloat("_Mode", 0f);
            patternMaterial.SetColor("_EmissionColor", brightColor * 4f); // Even brighter for WebGL
            #endif
        }
        else if (shader.name.Contains("Legacy"))
        {
            // Legacy shader - use bright color
            patternMaterial.color = brightColor * 2f; // Make it extra bright
        }
        else
        {
            // Other shaders - just use the color
            patternMaterial.color = brightColor;
        }
        
        Debug.Log($"Simple material created with shader: {shader.name}, tiling: {tiling}, brightness: {brightness}");
    }
    
    /// <summary>
    /// Load the pattern texture from Resources with WebGL compatibility
    /// </summary>
    public void LoadPatternTexture(string textureName)
    {
        patternTexture = Resources.Load<Texture2D>($"UI/GPT/{textureName}");
        
        if (patternTexture != null)
        {
            if (patternMaterial != null)
            {
                patternMaterial.mainTexture = patternTexture;
            }
            else
            {
                CreateBackgroundQuad();
            }
            
            textureLoaded = true;
            Debug.Log($"Loaded pattern texture: {textureName}");
        }
        else
        {
            Debug.LogError($"Failed to load pattern texture: {textureName}");
        }
    }
    
    /// <summary>
    /// Update the pattern color and transparency
    /// </summary>
    public void UpdatePatternAppearance(Color color, float alpha)
    {
        patternColor = new Color(color.r, color.g, color.b, alpha);
        
        if (patternMaterial != null)
        {
            Color brightColor = patternColor * brightness;
            patternMaterial.color = brightColor;
            
            // Update emission if using Standard shader
            if (patternMaterial.shader.name.Contains("Standard"))
            {
                patternMaterial.SetColor("_EmissionColor", brightColor * 2f);
                patternMaterial.SetColor("_Color", brightColor);
            }
        }
    }
    
    /// <summary>
    /// Set the brightness multiplier
    /// </summary>
    public void SetBrightness(float newBrightness)
    {
        brightness = newBrightness;
        UpdatePatternAppearance(patternColor, patternColor.a);
    }
    
    /// <summary>
    /// Force the material to be bright (useful for WebGL)
    /// </summary>
    public void ForceBrightness()
    {
        if (patternMaterial != null)
        {
            Color brightColor = patternColor * brightness;
            
            // Force bright settings regardless of shader
            patternMaterial.color = brightColor;
            
            // Try to set emission for Standard shader
            if (patternMaterial.shader.name.Contains("Standard"))
            {
                patternMaterial.EnableKeyword("_EMISSION");
                patternMaterial.SetColor("_EmissionColor", brightColor * 4f);
                patternMaterial.SetColor("_Color", brightColor);
                patternMaterial.SetFloat("_Glossiness", 0f);
                patternMaterial.SetFloat("_Metallic", 0f);
                patternMaterial.SetFloat("_Smoothness", 0f);
            }
            
            Debug.Log($"Forced brightness: {brightColor}, shader: {patternMaterial.shader.name}");
        }
    }
    
    /// <summary>
    /// Set the tiling amount
    /// </summary>
    public void SetTiling(Vector2 newTiling)
    {
        tiling = newTiling;
        
        if (patternMaterial != null)
        {
            patternMaterial.mainTextureScale = tiling;
        }
    }
    
    /// <summary>
    /// Set the game area size
    /// </summary>
    public void SetGameAreaSize(Vector2 size)
    {
        gameAreaSize = size;
        
        if (backgroundQuad != null)
        {
            backgroundQuad.transform.localScale = new Vector3(size.x, size.y, 1f);
        }
    }
    
    /// <summary>
    /// Force refresh the material (useful for WebGL debugging)
    /// </summary>
    public void RefreshMaterial()
    {
        if (patternMaterial != null && patternTexture != null)
        {
            patternMaterial.mainTexture = patternTexture;
            patternMaterial.mainTextureScale = tiling;
            patternMaterial.mainTextureOffset = Vector2.zero;
            patternMaterial.color = patternColor;
            
            Debug.Log($"Refreshed material with tiling: {tiling}");
        }
    }
    
    /// <summary>
    /// Check and log texture import settings (for debugging)
    /// </summary>
    public void CheckTextureSettings()
    {
        if (patternTexture != null)
        {
            Debug.Log($"Texture: {patternTexture.name}");
            Debug.Log($"Wrap Mode: {patternTexture.wrapMode}");
            Debug.Log($"Filter Mode: {patternTexture.filterMode}");
            Debug.Log($"Size: {patternTexture.width}x{patternTexture.height}");
            Debug.Log($"Is Readable: {patternTexture.isReadable}");
        }
        else
        {
            Debug.LogWarning("No texture assigned!");
        }
    }
    
    private void OnValidate()
    {
        if (Application.isPlaying && patternMaterial != null)
        {
            patternMaterial.mainTextureScale = tiling;
            patternMaterial.color = patternColor;
        }
    }
    
    private void OnDestroy()
    {
        if (patternMaterial != null)
        {
            DestroyImmediate(patternMaterial);
        }
    }
} 