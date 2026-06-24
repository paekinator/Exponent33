using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class BarFill : MonoBehaviour
{
    public Image Bar;
    public Image Fill;
    public TMP_Text HPText;
    public float currentFill, maxFill;
    public Gradient HPGradient;

    private void Awake() {
        currentFill = maxFill;
    }

    private void LateUpdate() {
        FillUpdater();
    }

    private void FillUpdater() {
        Fill.fillAmount = (float)currentFill/maxFill;
        Fill.color = HPGradient.Evaluate(Fill.fillAmount);
        HPText.text = Mathf.Ceil(currentFill).ToString();
    }

    public void UpdateFill(int Newfill) {
        currentFill = Newfill;
    }
}
