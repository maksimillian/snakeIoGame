using UnityEngine;

public class DeathEffectManager : MonoBehaviour
{
    public static DeathEffectManager Instance { get; private set; }
    
    [Header("Death Effect Prefabs")]
    public GameObject deathEffectPrefab;
    public GameObject segmentEffectPrefab;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void CreateDeathEffect(Vector3 position, int playerId)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"DeathEffectManager: Creating death effect for player {playerId} at position {position}");
        #endif
        
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, position, Quaternion.identity);
            effect.name = $"DeathEffect_{playerId}";
            Destroy(effect, 3f);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"DeathEffectManager: Instantiated death effect prefab for player {playerId}");
            #endif
        }
        else
        {
            // Fallback to simple effect if no prefab
            CreateSimpleDeathEffect(position, playerId);
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"DeathEffectManager: Created fallback death effect for player {playerId}");
            #endif
        }
    }
    
    public void CreateSegmentEffect(Vector3 position, int playerId)
    {
        if (segmentEffectPrefab != null)
        {
            GameObject effect = Instantiate(segmentEffectPrefab, position, Quaternion.identity);
            effect.name = $"SegmentEffect_{playerId}";
            Destroy(effect, 2f);
        }
        else
        {
            // Fallback to simple effect if no prefab
            CreateSimpleSegmentEffect(position, playerId);
        }
    }
    
    private void CreateSimpleDeathEffect(Vector3 position, int playerId)
    {
        GameObject deathEffect = new GameObject($"SimpleDeathEffect_{playerId}");
        deathEffect.transform.position = position;
        
        ParticleSystem particles = deathEffect.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = 2.5f;
        main.startSpeed = 4f;
        main.startSize = 0.3f;
        main.maxParticles = 30;
        main.startColor = new Color(1f, 0.4f, 0.2f); // Orange-red
        
        var emission = particles.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.8f;
        
        // Simple size over lifetime
        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        
        Destroy(deathEffect, 3f);
    }
    
    private void CreateSimpleSegmentEffect(Vector3 position, int playerId)
    {
        GameObject segmentEffect = new GameObject($"SimpleSegmentEffect_{playerId}");
        segmentEffect.transform.position = position;
        
        ParticleSystem particles = segmentEffect.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = 1.5f;
        main.startSpeed = 3f;
        main.startSize = 0.15f;
        main.maxParticles = 8;
        main.startColor = new Color(1f, 1f, 0.2f); // Bright yellow
        
        var emission = particles.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 8) });
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;
        
        Destroy(segmentEffect, 2f);
    }
} 