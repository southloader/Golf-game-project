using UnityEngine;

public class WindManager : MonoBehaviour
{
    [Header("바람 설정 (각도와 파워)")]
    [Range(0f, 360f)] 
    public float windAngle = 0f;    // 바람이 부는 방향 (0~360도)
    public float windPower = 0f;    // 바람의 세기

    [Header("랜덤 생성 설정")]
    public float maxWindPower = 5f; // 랜덤 생성 시 최대 파워

    [Header("현재 적용된 바람 벡터 (자동 계산 확인용)")]
    public Vector3 currentWindVector;

    [Header("연결할 골프공")]
    public CustomPhysicsBall golfBall;

    void Start()
    {
        // 게임 시작 시 랜덤한 각도와 파워의 바람 생성
        GenerateRandomWind();
    }

    // 테스트를 위해 에디터에서 각도/파워를 조절하면 실시간으로 공에 적용되도록 Update 사용
    void Update()
    {
        ApplyWindToBall();
    }

    public void GenerateRandomWind()
    {
        // 0도 ~ 360도 사이의 랜덤한 방향 지정
        windAngle = Random.Range(0f, 360f);
        
        // 0 ~ 최대치 사이의 랜덤한 바람 세기 지정
        windPower = Random.Range(0f, maxWindPower);

        ApplyWindToBall();
    }

    private void ApplyWindToBall()
    {
        // 1. 각도(windAngle)를 유니티의 3D 방향 벡터로 변환
        // Y축을 기준으로 회전시키면 수평(X, Z) 방향의 화살표가 만들어짐
        Vector3 windDirection = Quaternion.Euler(0f, windAngle, 0f) * Vector3.forward;

        // 2. 방향 벡터에 파워(크기)를 곱해서 최종 바람 벡터 완성
        currentWindVector = windDirection * windPower;

        // 3. 계산된 벡터를 골프공의 물리 코어에 전달
        if (golfBall != null)
        {
            golfBall.windForce = currentWindVector;
        }
    }
}