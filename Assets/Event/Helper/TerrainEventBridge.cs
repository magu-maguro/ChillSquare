using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TerrainEventBridge : MonoBehaviour
{
    [SerializeField] private List<TilemapRenderer> tilemapRenderers = new List<TilemapRenderer>(3);
    [SerializeField] private Material defaultMaterial;

    public void ApplyTerrainEffect(TerrainEventEffect terrainEffect)
    {
        if (terrainEffect == null)
        {
            Debug.LogWarning("TerrainEventEffect is null. No changes applied.");
            return;
        }

        foreach (var renderer in tilemapRenderers)
        {
            if (renderer != null)
            {
                renderer.material = terrainEffect.terrainMaterial;
            }
        }
    }

    public void ResetTerrainEffect()
    {
        foreach (var renderer in tilemapRenderers)
        {
            if (renderer != null)
            {
                renderer.material = defaultMaterial;
            }
        }
    }
}
