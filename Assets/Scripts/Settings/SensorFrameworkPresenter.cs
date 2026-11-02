using TMPro;
using UnityEngine;
namespace RhythmRehab
{
    public sealed class SensorFrameworkPresenter : MonoBehaviour
    {
        [SerializeField] private TMP_Text status;
        public void Scan()=>status.text="Device scanning is not implemented yet.\nThe sensor firmware and Bluetooth integration will be connected here.";
        public void ExplainCalibration()=>status.text="Waiting for sensor integration.\nNo reference angles or calibration data have been recorded.";
    }
}
