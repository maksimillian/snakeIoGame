using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public float gameAreaWidth = 200f; // Match server's game area size
    public float gameAreaHeight = 200f; // Match server's game area size
    public float wallThickness = 1f;

    [Header("References")]
    public Transform gameArea;
    public Transform walls;

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

    private void Start()
    {
        InitializeGameArea();
    }

    private void InitializeGameArea()
    {
        // Create game area boundaries
        CreateWalls();
    }

    private void CreateWalls()
    {
        // Create four walls around the game area
        CreateWall("TopWall", new Vector3(0, gameAreaHeight/2, 0), new Vector3(gameAreaWidth + wallThickness*2, wallThickness, 1));
        CreateWall("BottomWall", new Vector3(0, -gameAreaHeight/2, 0), new Vector3(gameAreaWidth + wallThickness*2, wallThickness, 1));
        CreateWall("LeftWall", new Vector3(-gameAreaWidth/2, 0, 0), new Vector3(wallThickness, gameAreaHeight, 1));
        CreateWall("RightWall", new Vector3(gameAreaWidth/2, 0, 0), new Vector3(wallThickness, gameAreaHeight, 1));
    }

    private void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = new GameObject(name);
        wall.transform.parent = walls;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        
        // Set the tag to "Wall" to fix collision detection
        wall.tag = "Wall";
        
        // Add collider
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        
        // Add visual
        SpriteRenderer renderer = wall.AddComponent<SpriteRenderer>();
        renderer.color = Color.gray;
    }
} 