
public interface IAdjustConfig{
    float GetTotalDuration(int characterCount);

    void AdjustDuration(float availableDuration, int characterCount);
}