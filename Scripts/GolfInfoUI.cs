using UnityEngine;
using TMPro;

public class GolfInfoUI : MonoBehaviour
{
    [Header("References")]
    public CustomPhysicsBall ball;
    public TMP_Text infoText;

    [Header("Display")]
    public bool showKorean = true;
    public bool showWindDirectionText = false;

    void Update()
    {
        if (ball == null || infoText == null) return;

        int stroke = ball.strokeCount;

        Vector3 wind = ball.windForce;
        Vector3 flatWind = new Vector3(wind.x, 0f, wind.z);
        float windSpeed = flatWind.magnitude;

        if (showKorean)
        {
            if (showWindDirectionText)
            {
                infoText.text =
                    $"타수: {stroke}\n" +
                    $"풍속: {windSpeed:F1}\n" +
                    $"풍향: {GetWindDirectionName(flatWind)}";
            }
            else
            {
                infoText.text =
                    $"타수: {stroke}\n" +
                    $"풍속: {windSpeed:F1}";
            }
        }
        else
        {
            if (showWindDirectionText)
            {
                infoText.text =
                    $"Stroke: {stroke}\n" +
                    $"Wind: {windSpeed:F1}\n" +
                    $"Dir: {GetWindDirectionName(flatWind)}";
            }
            else
            {
                infoText.text =
                    $"Stroke: {stroke}\n" +
                    $"Wind: {windSpeed:F1}";
            }
        }
    }

    private string GetWindDirectionName(Vector3 flatWind)
    {
        if (flatWind.magnitude < 0.01f)
            return showKorean ? "없음" : "None";

        float angle = Mathf.Atan2(flatWind.x, flatWind.z) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;

        if (showKorean)
        {
            if (angle >= 337.5f || angle < 22.5f) return "북";
            if (angle < 67.5f) return "북동";
            if (angle < 112.5f) return "동";
            if (angle < 157.5f) return "남동";
            if (angle < 202.5f) return "남";
            if (angle < 247.5f) return "남서";
            if (angle < 292.5f) return "서";
            return "북서";
        }
        else
        {
            if (angle >= 337.5f || angle < 22.5f) return "N";
            if (angle < 67.5f) return "NE";
            if (angle < 112.5f) return "E";
            if (angle < 157.5f) return "SE";
            if (angle < 202.5f) return "S";
            if (angle < 247.5f) return "SW";
            if (angle < 292.5f) return "W";
            return "NW";
        }
    }
}