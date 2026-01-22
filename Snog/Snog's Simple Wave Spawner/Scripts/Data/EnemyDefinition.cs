
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Snog/Simple Wave System/Enemy Definition", fileName = "Snog_EnemyDefinition")]
public class EnemyDefinition : ScriptableObject
{
    [Header("Core")]
    public GameObject prefab;

    [Min(1)]
    public int cost = 1;

    [Min(0f)]
    public float weight = 1f;

    [Header("Availability")]
    [Min(1)]
    public int minWave = 1;

    [Min(1)]
    public int maxWave = 999;

    [Header("Tags (Optional)")]
    public List<string> tags = new();

    public bool IsAvailableForWave(int wave)
    {
        return wave >= minWave && wave <= maxWave && prefab != null && cost > 0 && weight > 0f;
    }

    public bool HasTag(string tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return false;
        }

        return tags != null && tags.Contains(tag);
    }
}
