using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace cs5678_2024sp.h_balloon_selection.hinkle.gdh55
{

    /// <summary>
    /// This component handles user input and updates the BalloonSelection component accordingly.
    /// </summary>
    public class BalloonSelectionInput : MonoBehaviour
    {
        [SerializeField] private InputActionProperty m_NormalizedRadius;
        
        private BalloonSelection m_BalloonSelection; //reference to the balloon selection component

        /// <summary>
        /// Input action property for gathering user input to change the balloon radius.
        /// The input action provides a value between 0-1.
        /// </summary>
        public InputActionProperty normalizedRadius
        {
            get => m_NormalizedRadius;
            set => m_NormalizedRadius = value;
        }
        
        // Start is called before the first frame update
        void Start()
        {
            //set the normalized radius input action to the balloon selection's normalized radius input action
            //m_NormalizedRadius.action.performed += OnNormalizedRadius;
            m_BalloonSelection = GetComponent<BalloonSelection>();
            m_NormalizedRadius.action.Enable();  
        }
        
        // Update is called once per frame
        void Update()
        {
            
            float radius = m_NormalizedRadius.action.ReadValue<float>();
            float invertPinchValue = 1 - radius;
            Debug.Log("pinch value being sent to balloonSelection.setNrormalizeRadius == " + invertPinchValue);
            m_BalloonSelection.SetNormalizedRadius(invertPinchValue);


        }
 
    }
}