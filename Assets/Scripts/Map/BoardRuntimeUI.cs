using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용 시

public class BoardRuntimeUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider mainPathLengthSlider;
    public TMP_Text mainPathLengthValue;

    public Slider branchChanceSlider;
    public TMP_Text branchChanceValue;

    public Slider branchIntervalSlider;
    public TMP_Text branchIntervalValue;

    public Slider specialTileCountSlider;
    public TMP_Text specialTileCountValue;

    public Button regenerateButton;

    private BoardGenerator boardGenerator;

    void Start()
    {
        boardGenerator = FindObjectOfType<BoardGenerator>();

        if (boardGenerator == null)
        {
            Debug.LogError("❌ BoardGenerator를 찾을 수 없습니다. Scene에 존재하는지 확인하세요.");
            return;
        }

        // 슬라이더 초기값 설정
        InitSliders();
        AddListeners();
    }

    void InitSliders()
    {
        mainPathLengthSlider.value = boardGenerator.mainPathLength;
        mainPathLengthValue.text = boardGenerator.mainPathLength.ToString();

        branchChanceSlider.value = boardGenerator.branchChance;
        branchChanceValue.text = boardGenerator.branchChance.ToString("F2");

        branchIntervalSlider.value = boardGenerator.branchInterval;
        branchIntervalValue.text = boardGenerator.branchInterval.ToString("F1");

        specialTileCountSlider.value = TotalSpecialTileCount();
        specialTileCountValue.text = specialTileCountSlider.value.ToString();
    }

    void AddListeners()
    {
        mainPathLengthSlider.onValueChanged.AddListener(v =>
        {
            boardGenerator.mainPathLength = Mathf.RoundToInt(v);
            mainPathLengthValue.text = v.ToString("F0");
        });

        branchChanceSlider.onValueChanged.AddListener(v =>
        {
            boardGenerator.branchChance = v;
            branchChanceValue.text = v.ToString("F2");
        });

        branchIntervalSlider.onValueChanged.AddListener(v =>
        {
            boardGenerator.branchInterval = v;
            branchIntervalValue.text = v.ToString("F1");
        });

        specialTileCountSlider.onValueChanged.AddListener(v =>
        {
            UpdateRegionSpecialCounts(Mathf.RoundToInt(v));
            specialTileCountValue.text = v.ToString("F0");
        });

        regenerateButton.onClick.AddListener(() =>
        {
            boardGenerator.RegenerateWithRuntimeSettings();
        });
    }

    int TotalSpecialTileCount()
    {
        int total = 0;
        foreach (var region in boardGenerator.terrainRegions)
            total += region.specialTileCount;
        return total;
    }

    void UpdateRegionSpecialCounts(int total)
    {
        if (boardGenerator.terrainRegions.Count == 0) return;
        int perRegion = Mathf.Max(1, total / boardGenerator.terrainRegions.Count);

        for (int i = 0; i < boardGenerator.terrainRegions.Count; i++)
            boardGenerator.terrainRegions[i].specialTileCount = perRegion;
    }
}
