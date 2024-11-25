using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


namespace cs5678_2024sp.h_balloon_selection.hinkle.gdh55
{
    /// <summary>
    /// This component provides visual and auditory feedback for the BalloonSelection technique.
    /// </summary>
    public class BalloonSelectionFeedback : MonoBehaviour
    {
        
        [SerializeField] private Material m_BallooonMaterial;
        [SerializeField] private Material m_LineMaterial;
        [SerializeField] private AudioClip m_RatchetAudio;
        [SerializeField] private float m_RatchetThreshold = 0.5f;
        
        
        
        private BalloonSelection m_BalloonSelection; //reference to the balloon selection component
        private LineRenderer m_LineRendererFingerToFinger;
        private LineRenderer m_LineRendererFingerToBalloonToAnchor;
        
        private GameObject m_AnchorSphereVisualizer;
        private GameObject m_StretchingSphereVisualizer;
        
        void Awake()
        {
            m_BalloonSelection = GetComponent<BalloonSelection>();
        }
        
        // Start is called before the first frame update
        void Start()
        {
            
            // Create the sphere visualizers for the anchor and stretching transforms
            CreateVisualizationSpheres();
            
            // Create the Line Renderer between the anchor and the stretching transform and
            // between the anchor transform and the balloon
            CreateLineRenderers();
            
        }
        

        // Update is called once per frame
        void Update()
        {
            if(m_BalloonSelection.balloonState != BalloonSelection.BalloonState.Idle)
            {
                m_LineRendererFingerToFinger.enabled = true;
                m_LineRendererFingerToBalloonToAnchor.enabled = true;
                //update the line renderer
                UpdateLineRenderer();
            }
            else
            {
                m_LineRendererFingerToFinger.enabled = false;
                m_LineRendererFingerToBalloonToAnchor.enabled = false;
            }
            
            //add audio source component to BalloonSelection.anchor
        }
        
        
        // Create the visualization spheres on the anchor and stretching transforms
        private void CreateVisualizationSpheres()
        {
            // Create the anchor visualization sphere dynamically
            m_AnchorSphereVisualizer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_AnchorSphereVisualizer.name = "AnchorSphereVisualizer";
            m_AnchorSphereVisualizer.transform.position = m_BalloonSelection.anchor.position;
            m_AnchorSphereVisualizer.transform.SetParent(m_BalloonSelection.anchor, false);
            m_AnchorSphereVisualizer.transform.localPosition = Vector3.zero;
            m_AnchorSphereVisualizer.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);
            m_AnchorSphereVisualizer.GetComponent<MeshRenderer>().material = m_BallooonMaterial;
            var anchorRb = m_AnchorSphereVisualizer.AddComponent<Rigidbody>();
            anchorRb.isKinematic = true;
            anchorRb.useGravity = false;
      

            // Create the stretching visualization sphere dynamically
            m_StretchingSphereVisualizer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_StretchingSphereVisualizer.name = "StretchingSphereVisualizer";
            m_StretchingSphereVisualizer.transform.position = m_BalloonSelection.stretching.position;
            m_StretchingSphereVisualizer.transform.SetParent(m_BalloonSelection.stretching, false);
            //set local position to 0,0,0
            m_StretchingSphereVisualizer.transform.localPosition = Vector3.zero;
            m_StretchingSphereVisualizer.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);
            m_StretchingSphereVisualizer.GetComponent<MeshRenderer>().material = m_BallooonMaterial;
            var stretchingRb = m_StretchingSphereVisualizer.AddComponent<Rigidbody>();
            stretchingRb.isKinematic = true;
            stretchingRb.useGravity = false;
        }
        
        /// <summary>
        /// Create a line renderer from the anchor to the stretching transform and from the stretching transform to the balloon.
        /// </summary>
        private void CreateLineRenderers()
        {
            //-----------------------------------LR between anchor and stretching transform---------------------------------------------------
            //set the line renderer's material to the line material
            //create a line renderer component
            
            m_LineRendererFingerToFinger = m_BalloonSelection.stretching.transform.gameObject.AddComponent<LineRenderer>();
            m_LineRendererFingerToFinger.material = m_LineMaterial;
            //tile the texture
            m_LineRendererFingerToFinger.textureMode = LineTextureMode.Tile;
            
            //set the line renderer's width
            m_LineRendererFingerToFinger.startWidth = 0.01f;
            m_LineRendererFingerToFinger.endWidth = 0.01f;
            //set the line renderer's positions (one on the anchor and one on the stretching transform)
            m_LineRendererFingerToFinger.positionCount = 2;
            
            // set the line renderer texture tiling to be proportional to the number of segments in the line renderer -- this should probably be updated to make it based on the distance
            float tiling = 40f;
            m_LineRendererFingerToFinger.material.mainTextureScale = new Vector2(m_LineRendererFingerToFinger.positionCount * tiling , 1);
            //set the line renderer's positions (one on the anchor and one on the stretching transform)
            m_LineRendererFingerToFinger.SetPosition(0, m_BalloonSelection.anchor.position);
            m_LineRendererFingerToFinger.SetPosition(1, m_BalloonSelection.stretching.position);
            
            //-----------------------------------LR between anchor and Balloon---------------------------------------------------
            
            //CREATE THE LINE RENDERER BETWEEN THE ANCHOR TRANSFORM AND THE BALLOON
            m_LineRendererFingerToBalloonToAnchor = m_BalloonSelection.anchor.transform.gameObject.AddComponent<LineRenderer>();
            //set the line renderer's material to the line material
            m_LineRendererFingerToBalloonToAnchor.material = m_LineMaterial;
            //tile the texture
            m_LineRendererFingerToBalloonToAnchor.textureMode = LineTextureMode.Tile;
            //set the line renderer's width
            m_LineRendererFingerToBalloonToAnchor.startWidth = 0.01f;
            m_LineRendererFingerToBalloonToAnchor.endWidth = 0.01f;
            //set the line renderer's positions (one on the anchor and one on the balloon)
            m_LineRendererFingerToBalloonToAnchor.positionCount = 2;
            // set the line renderer texture tiling to be proportional to the number of segments in the line renderer -- this should probably be updated to make it based on the distance
            m_LineRendererFingerToBalloonToAnchor.material.mainTextureScale = new Vector2(m_LineRendererFingerToBalloonToAnchor.positionCount * tiling , 1);
            //set the line renderer's positions (one on the anchor and one on the balloon)
            m_LineRendererFingerToBalloonToAnchor.SetPosition(0, m_BalloonSelection.anchor.position);
            m_LineRendererFingerToBalloonToAnchor.SetPosition(1, m_BalloonSelection.balloonPosition);
            
        }
        
        

        /// <summary>
        /// Update the Line renderer between the anchor and stretching transform and between the stretching transform and the balloon.
        /// </summary>
        private void UpdateLineRenderer()
        {
            //update the line renderer between the anchorsphere and the and the stretchingsphere transform 
            m_LineRendererFingerToFinger.SetPosition(0, m_BalloonSelection.anchor.position);
            m_LineRendererFingerToFinger.SetPosition(1, m_BalloonSelection.stretching.position);
            
            //update the line renderer between the stretching transform and the balloon
            m_LineRendererFingerToBalloonToAnchor.SetPosition(0, m_BalloonSelection.anchor.position);
            m_LineRendererFingerToBalloonToAnchor.SetPosition(1, m_BalloonSelection.balloonPosition);
            
        }
        

        //-----------------------------------------------------------------------------------------------------
        //-----------------------------Properties---------------------------------------
        //-----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Material applied to the balloon and spheres representing the anchor and stretching game objects.
        /// </summary>
        public Material balloonMaterial
        {
            get => m_BallooonMaterial;
        }

        /// <summary>
        /// Material for the line renderer used to draw the balloon string.
        /// </summary>
        public Material lineMaterial
        {
            get => m_LineMaterial;
        }

        /// <summary>
        /// Audio clip to be played for ratchet auditory feedback, as demonstrated
        /// in the original paper.
        /// </summary>
        public AudioClip ratchetAudio
        {
            get => m_RatchetAudio;
        }


        /// <summary>
        /// Threshold that defines the change in distance between anchor and stretching needed to trigger playback of the ratchetAudio clip.
        /// </summary>
        public float ratchetThreshold
        {
            get => m_RatchetThreshold;
        }
    }
}