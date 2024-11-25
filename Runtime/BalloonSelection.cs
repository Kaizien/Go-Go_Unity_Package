using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.Interaction.Toolkit;



namespace cs5678_2024sp.h_balloon_selection.hinkle.gdh55
{
    public class BalloonSelection : MonoBehaviour
    {
        [SerializeField] private XRBaseInteractor m_Interactor;
        [SerializeField] private Transform m_Anchor;
        [SerializeField] private Transform m_Stretching;             // left index tip
        [SerializeField] private float m_ContactThreshold = 0.022f; // how close the index tip needs to be to the other index tip in order to activate the balloon.
        [SerializeField] private float m_MinBalloonRadius = 0.02f; // minimum size of the balloon
        [SerializeField] private float m_MaxBalloonRadius = 0.1f; // maximum size of the balloon
        
        private GameObject m_Balloon; // Balloon game object that we will instantiate
        private float m_CurrentDistance;   // current distance with respect to the total distance between the hands.
        private float m_PreviousDistance = 0f; //previous distance between the anchor and the stretching transform

        
        // Vars related to moving average
        private float m_MovingAverageSize = 20; //size of the moving average array
        public Queue<float> m_MovingAverageQueue = new Queue<float>(); //queue to store the measured distances to be used for moving avg calculations
        private float m_MovingAvgDistance; // calculated moving average distance corresponding to the change in distance since the last frame
        private float m_RatchetDistanceTracker = 0f; // Audio Related 
        
        private Vector3 m_BalloonPosition; //The world position of the balloon
        private float m_BalloonRadius;        //The radius of the balloon
        private float m_NormalizedBalloonRadius;         //normalized radius of the balloon
        private BalloonState m_BalloonState;
        
        
        //reference to BalloonSelectionFeedback.cs
        private BalloonSelectionFeedback m_BalloonFeedback;
        
        
        /// <summary>
        /// The state of of the selection technique. The field names are derived from the original paper. The sequence of states is as follows: Idle -> Stretching -> InUse .
        /// </summary>
        public enum BalloonState
        {
            /// <summary>
            /// When the user has not initiated the technique.
            /// </summary>
            Idle,  
            /// <summary>
            /// This is the state entered after user contracts the line during Stretching state, therefore fixing the total length of the balloon string and moving the balloon up and down accordingly.
            /// </summary>
            InUse, 
            /// <summary>
            /// The state when user is stretching the string, after initiating the use of the technique. The technique will remain in this state until the user starts contracting the string.
            /// </summary>
            Stretching
        }
        

        
        // Start is called before the first frame update
        void Start()
        {
            m_BalloonFeedback = GetComponent<BalloonSelectionFeedback>();
            m_BalloonState = BalloonState.Idle;
            CreateBalloon();  //create the balloon game object
        }
        
        // Update is called once per frame
        void Update()
        {
            IsPalmUp(); // check to see if the palm is up. if it is, then the balloon state is idle
            if (m_BalloonState == BalloonState.Idle)
            {
                //check to see if the distance between the anchor and the stretching transform is less than the contact threshold
                if (Vector3.Distance(m_Anchor.position, m_Stretching.position) < m_ContactThreshold)
                {
                    BalloonSelectionActivated();
                }
            }
            
            // get the moving average of the distance between the anchor and the stretching transform -- simple moving average
            // store change in distance from frame to frame and then average it out.
            // dont use "linq" for this! its useful, but it has garbage collection issues.
            if (m_BalloonState != BalloonState.Idle)
            {
                m_Balloon.SetActive(true);
                CalculateLineDistance();          // Calculate the distance between the anchor and the stretching transform using a moving average.
            }

        }
        

        /// <summary>
        /// When BalloonSelection is Activated, set the balloon to active and set its position to the anchor's position;
        /// set the BalloonState to InUse
        /// Reset the moving average distance to the current distance and set the previous distance to the current distance
        /// Set all other distances in the moving distances array to the current distance
        /// </summary>
        private void BalloonSelectionActivated()
        {
            m_Balloon.SetActive(true);
            m_Balloon.transform.localPosition = new Vector3(0,0,0); // set the balloon position to the anchor's position
            m_BalloonPosition = m_Balloon.transform.position; // set the balloonposition var to the balloon's position
            m_Interactor.transform.SetParent(m_Balloon.transform); // set the interactor's parent to the balloon
            m_BalloonState = BalloonState.InUse;  //change the balloonstate
        }

        /// <summary>
        /// create the balloon game object then set it to inactive
        /// </summary>
        private void CreateBalloon()
        {
            //instantiate a new game object for the balloon that is a 3D Sphere  with a sphere collider, rigid body, mesh renderer, and scale of m_MinBalloonRadius
            m_Balloon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //name the m_balloon game object "balloon"
            m_Balloon.name = "Balloon";
            // add rigid body to the balloon
            Rigidbody rb = m_Balloon.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            m_Balloon.GetComponent<MeshRenderer>().enabled = true;
            m_Balloon.GetComponent<SphereCollider>().isTrigger = true;
            m_Balloon.GetComponent<SphereCollider>().radius = m_MinBalloonRadius;
            m_Balloon.GetComponent<Renderer>().material = m_BalloonFeedback.balloonMaterial;
            m_Balloon.transform.position = m_Anchor.position;
            //m_Balloon.transform.SetParent(anchor); //set the balloon's parent to the anchor
            m_BalloonPosition = m_Balloon.transform.position;
            m_Balloon.transform.localScale = new Vector3(m_MinBalloonRadius, m_MinBalloonRadius, m_MinBalloonRadius);
            m_Balloon.SetActive(false);
        }
        
        /// <summary>
        /// Calculate the moving average of the distances between the anchor and the stretching transform since the last distance measurement. The 'delta' distance change.
        /// </summary>
        /// <returns>Current Moving Average</returns>
        private float CalculateMovingAverage()
        {
            float sum = 0;
            foreach(float x in m_MovingAverageQueue)
            {
                sum += x;
            }
            
            return (sum / m_MovingAverageQueue.Count);
        }

        /// <summary>
        /// Calculates the current distance bewteen Anchor and the Stretching Transforms. Adds measurement to the queue, then
        /// calculates the the moving average using CalculateMovingAverage(). Uses moving average calculation to call UpdateBalloon()
        /// </summary>
        private void CalculateLineDistance()
        {
            
            m_CurrentDistance = Vector3.Distance(m_Anchor.position, m_Stretching.position); // Step 1: Get the current distance between the anchor and the stretching transform
            
            float distanceDelta = m_CurrentDistance - m_PreviousDistance; // calculate the change in distance between the current distance and the previous distance
            
            m_MovingAverageQueue.Enqueue(distanceDelta);                                     // Step 2: Add the current distance to the moving average queue
            if (m_MovingAverageQueue.Count > m_MovingAverageSize)                                //Check the queue and remove the first element if the queue size is greater than the moving average size
            {
                m_MovingAverageQueue.Dequeue(); 
            }
            m_MovingAvgDistance = CalculateMovingAverage();                                      // Step 3: Calculate the moving average of the distances
            if(m_MovingAvgDistance > 0){m_BalloonState = BalloonState.Stretching;} // if the moving average distance is greater than 0, then the balloon state is stretching
            else { m_BalloonState = BalloonState.InUse; }                          // if the moving average distance is less than 0, then the balloon state is in use

            Vector3 newPosition = m_Balloon.transform.position;                                 // Step 4: Validate the new position of the balloon based on the moving average distance is within appropriate range.
            newPosition.y -= m_MovingAvgDistance;                                               // invert the balloon position based on the moving average distance
            
            newPosition.y = Mathf.Clamp(newPosition.y, anchor.position.y, anchor.position.y + 3f); // max rise of 3f from the anchor position was arbitrarily chosen
            newPosition.x = anchor.position.x;
            newPosition.z = anchor.position.z;
            m_Balloon.transform.position = newPosition;                                           // update the balloon's position
            m_BalloonPosition = m_Balloon.transform.position;                                  // Step 5: set the BalloonPosition variable after the balloon's position has been updated
            m_PreviousDistance = m_CurrentDistance;                                            // store previous distance
            
            
            m_RatchetDistanceTracker += Mathf.Abs(m_MovingAvgDistance);                       // Step 6: Play audio clip if the ratchet distance tracker is greater than the ratchet threshold
            if (m_RatchetDistanceTracker > m_BalloonFeedback.ratchetThreshold)
            {
                PlayAudioFeedback();
            }
            
            
        }
        
        
        /// <summary>
        /// Play the ratchet audio from the feedback script. Triggered when distance between anchor and stretching transform > ratchet threshold
        /// </summary>
        private void PlayAudioFeedback()
        {
            //play the audio clip from the feedback script and play it. we get the audio clip from m_BalloonFeedback.RatchetAudio()
            AudioSource.PlayClipAtPoint(m_BalloonFeedback.ratchetAudio, m_BalloonPosition);
            Debug.Log("Playing Audio clip!");
            m_RatchetDistanceTracker = 0f; // reset the ratchet distance tracker
            
        }

        

        /// <summary>
        /// The transform of the anchor game object. This corresponds to the "anchor finger" mentioned in the original paper.
        /// </summary>
        public Transform anchor
        {
            get => m_Anchor;
            set => m_Anchor = value;
        }

        /// <summary>
        /// The world position of the balloon.
        /// </summary>
        public Vector3 balloonPosition
        {
            get => m_BalloonPosition;
        }

        /// <summary>
        /// The radius of the balloon. This sets the radius of the interactor's sphere collider.
        /// For setting the balloon radius, use SetNormalizedRadius(Single).
        /// </summary>
        public float balloonRadius
        {
            get => m_BalloonRadius;
        }

        /// <summary>
        /// The current state of the technique
        /// </summary>
        public BalloonState balloonState
        {
            get => m_BalloonState;
        }
        
        
        
        /// <summary>
        /// Threshold for initiating the balloon selection technique.
        /// When the distance between anchor and stretching is below this threshold, the technique can be initiated.
        /// This allows the user to bring together the two game objects (for example fingers) to initiate the technique.
        /// </summary>
        public float contactThreshold
        {
            get => m_ContactThreshold;
            set => m_ContactThreshold = value;
        }

        /// <summary>
        /// The interactor responsible for selecting the object of interest.
        /// The interactor's attach transform and sphere collider are modified based on the balloonPosition and
        /// balloonRadius properties.
        /// </summary>
        public XRBaseInteractor interactor
        {
            get => m_Interactor;
            set => m_Interactor = value;
        }

        /// <summary>
        /// The maximum balloon radius
        /// </summary>
        public float maxBalloonRadius
        {
            get => m_MaxBalloonRadius;
            set => m_MaxBalloonRadius = value;
        }

        
        /// <summary>
        /// The minimum balloon radius.
        /// </summary>
        public float minBalloonRadius
        {
            get => m_MinBalloonRadius;
            set => m_MinBalloonRadius = value;
        }

        /// <summary>
        /// The normalized balloon radius (between 0 to 1), which represents the balloon radius as a
        /// value between minBalloonRadius and maxBalloonRadius.
        /// </summary>
        public float normalizedBalloonRadius
        {
            get => m_NormalizedBalloonRadius;
        }

        /// <summary>
        /// The transform of the stretching game object.
        /// This corresponds to the "stretching finger" mentioned in the original paper.
        /// </summary>
        public Transform stretching
        {
            get => m_Stretching;
            set => m_Stretching = value;
        }
        
        ///////////////////////////////Public Methods//////////////////////////////

        /// <summary>
        /// Sets the normalized radius of the balloon. The normalized radius determines the balloon radius by mapping between minBalloonRadius and maxBalloonRadius.
        /// </summary>
        /// <param name="normalizedRadius"></param>
        public void SetNormalizedRadius(float normalizedRadius)
        {
           // Debug.Log("Setting Normalized Radius Called with a value of " + normalizedRadius);
            // single normaliedRadius the normalized radius value to set, in the range of 0 to 1.
            m_NormalizedBalloonRadius = Mathf.Clamp(normalizedRadius, 0, 1); 
            m_BalloonRadius = Mathf.Lerp(m_MinBalloonRadius, m_MaxBalloonRadius, m_NormalizedBalloonRadius);
            m_Balloon.transform.localScale = new Vector3(m_BalloonRadius, m_BalloonRadius, m_BalloonRadius);
            m_Interactor.GetComponent<SphereCollider>().radius = m_BalloonRadius;
            // Debug.Log("Normalized Radius Set to " + m_NormalizedBalloonRadius + " and Balloon Radius Set to " + m_BalloonRadius);
        }
        
        //reset balloon select
        // use the dot product and vector3.up to see if the palm is facing up
        //dot product between the camera forward vector and the palm up vector
        //if the dot product is greater than 0.7, then the palm is facing up

        private void IsPalmUp()
        {
            float dotProduct = Vector3.Dot(Camera.main.transform.forward, m_Anchor.up);
            Debug.Log("DotProduct = " + dotProduct);
            if (dotProduct > 0.85f)
            {
                //if the dot product is greater than the threshold, then the palm is facing up
                m_BalloonState = BalloonState.Idle;
                m_Balloon.SetActive(false);
                m_Balloon.transform.position = m_Anchor.position;
                m_BalloonPosition = m_Balloon.transform.position;
                m_PreviousDistance = 0f;
                m_NormalizedBalloonRadius = 0f;
                m_BalloonRadius = minBalloonRadius;
                //m_Interactor.transform.position = m_Stretching.position;
                m_Interactor.transform.SetParent( GameObject.Find("Left Hand").transform);
                m_Interactor.transform.localPosition = new Vector3(0,0,0);
                m_MovingAverageQueue.Clear(); 
                


            }
        }
    }
}
