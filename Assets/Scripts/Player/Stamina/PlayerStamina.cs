using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina")]
    [SerializeField] private float maxStamina = 3f;

    [SerializeField] private float currentStamina = 3f;

    [SerializeField] private float regenerationPerSecond = 0.15f;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;

    private bool regenerationEnabled = true;

    private void Update()
    {
        if (!regenerationEnabled)
            return;

        Regenerate();
    }

    public void SetRegenerationEnabled(bool enabled)
    {
        regenerationEnabled = enabled;
    }

    private void Regenerate()
    {
        if (currentStamina >= maxStamina)
            return;

        currentStamina +=
            regenerationPerSecond *
            Time.deltaTime;

        currentStamina =
            Mathf.Min(currentStamina, maxStamina);
    }

    public bool HasAtLeast(float amount)
    {
        return currentStamina >= amount;
    }

    public bool TrySpend(float amount)
    {
        if (currentStamina < amount)
            return false;

        currentStamina -= amount;

        return true;
    }

    public void Drain(float amount)
    {
        currentStamina = Mathf.Max(0f, currentStamina - amount);
    }

    public void Restore(float amount)
    {
        currentStamina += amount;

        currentStamina =
            Mathf.Clamp(
                currentStamina,
                0f,
                maxStamina
            );
    }
}
