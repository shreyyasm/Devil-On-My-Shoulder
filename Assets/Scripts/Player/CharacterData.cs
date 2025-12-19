using UnityEngine;

[CreateAssetMenu(
    fileName = "CharacterData",
    menuName = "Character/Character Data",
    order = 0)]
public class CharacterData : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 4500f;

    [Header("Jump")]
    public float jumpForce = 550f;

    [Header("Slide")]
    public float slideForce = 15f;

    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Damage")]
    public float damageMultiplier = 100f;
}
