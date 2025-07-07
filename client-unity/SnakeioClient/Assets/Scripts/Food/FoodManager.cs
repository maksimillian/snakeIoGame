using UnityEngine;
using System.Collections.Generic;

public class FoodManager : MonoBehaviour
{
    public static FoodManager Instance { get; private set; }

    [Header("Food Settings")]
    public GameObject foodPrefab;
    public float spawnRadius = 40f;
    public int maxFoodCount = 50;
    public float spawnInterval = 1f;

    [Header("Food Types")]
    public Color defaultFoodColor = Color.green;

    private List<GameObject> activeFood = new List<GameObject>();
    private float lastSpawnTime;

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

    private void Update()
    {
        if (Time.time - lastSpawnTime >= spawnInterval && activeFood.Count < maxFoodCount)
        {
            SpawnFood();
            lastSpawnTime = Time.time;
        }
    }

    public void SpawnFood(Vector2? position = null, bool isBoost = false, float? size = null)
    {
        if (foodPrefab == null) return;

        Vector2 spawnPos;
        if (position.HasValue)
        {
            spawnPos = position.Value;
        }
        else
        {
            // Generate random position within game area
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(0f, spawnRadius);
            spawnPos = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                Mathf.Sin(angle * Mathf.Deg2Rad) * distance
            );
        }

        GameObject food = Instantiate(foodPrefab, spawnPos, Quaternion.identity);
        food.transform.parent = transform;

        // Set food size
        if (size.HasValue && size.Value != 1.0f)
        {
            food.transform.localScale = Vector3.one * size.Value;
        }

        // Set food color (will be overridden by server-provided color)
        SpriteRenderer renderer = food.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = defaultFoodColor;
            
            // Set sorting order to render food under snake sprites
            renderer.sortingOrder = -10; // Lower than snake segments which are 2-9
        }

        // Add food component
        Food foodComponent = food.GetComponent<Food>();
        if (foodComponent == null)
        {
            foodComponent = food.AddComponent<Food>();
        }
        foodComponent.Initialize(isBoost);

        activeFood.Add(food);
    }

    public void UpdateFoodSize(GameObject food, float size)
    {
        if (food != null)
        {
            food.transform.localScale = Vector3.one * size;
        }
    }

    public void RemoveFood(GameObject food)
    {
        if (activeFood.Contains(food))
        {
            activeFood.Remove(food);
            Destroy(food);
        }
    }

    public void ClearAllFood()
    {
        foreach (GameObject food in activeFood)
        {
            if (food != null)
            {
                Destroy(food);
            }
        }
        activeFood.Clear();
    }
} 