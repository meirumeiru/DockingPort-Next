using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using DockingFunctions;


namespace DockingPortNext.Module
{
	public class ModuleDockingPortEx : PartModule, IDockable, ITargetable, IModuleInfo, IResourceConsumer, IConstruction
	{
		// Settings

		[KSPField(isPersistant = false), SerializeField]
		public string nodeType = "DockingPort";

		[KSPField(isPersistant = false), SerializeField]
		private string nodeTypesAccepted = "DockingPort";

		public HashSet<string> nodeTypesAcceptedS = null;

		[KSPField(isPersistant = false), SerializeField]
		public int supportedModes = 3; // passive = 1, active = 2, both = 3


		[KSPField(isPersistant = false), SerializeField]
		public string nodeTransformName = "dockingNode";

		[KSPField(isPersistant = false), SerializeField]
		public string referenceAttachNode = ""; // if something is connected to this node, then the state is "Attached" (or "Pre-Attached" -> connected in the VAB/SPH)

		[KSPField(isPersistant = false), SerializeField]
		public Vector3 dockingOrientation = Vector3.up; // defines the direction of the docking port (when docked at a 0° angle, these local vectors of two ports point into the same direction)

		[KSPField(isPersistant = false), SerializeField]
		public int snapCount = 1;

		[KSPField(isPersistant = false), SerializeField]
		public string controlTransformName = "";

		[KSPField(isPersistant = false), SerializeField]
		public Vector3 controlOrientation = Vector3.up; // defines the direction of the control transform (often needed if dockingOrientation is used also)


		[KSPField(isPersistant = false), SerializeField]
		public string ringName = "";

		[KSPField(isPersistant = false), SerializeField]
		public Vector3 correctionVector = Vector3.zero; // offset of the "ring center" used in calculations from the real center of the ring-model

		[KSPField(isPersistant = false), SerializeField]
		public float extensionLength = 0.18f; // extension (while searching other port)
		
		[KSPField(isPersistant = false), SerializeField]
		public float maxExtensionLength = 0.25f; // maximum extension (used when in active push mode)

		[KSPField(isPersistant = false), SerializeField]
		public float extensionSpeed = 0.005f;

		[KSPField(isPersistant = false), SerializeField]
		public float pushSpeed = 0.01f;


		[KSPField(isPersistant = false), SerializeField]
		public float detectionDistance = 5f;

		[KSPField(isPersistant = false), SerializeField]
		public float approachingDistance = 1f;

		[KSPField(isPersistant = false), SerializeField]
		public float approachingAlignment = 15f;


		[KSPField(isPersistant = false), SerializeField]
		public float captureDistance = 0.005f;

		[KSPField(isPersistant = false), SerializeField]
		public float captureAlignment = 5f;

		[KSPField(isPersistant = false), SerializeField]
		public float captureAngle = 5f;


		[KSPField(isPersistant = false), SerializeField]
		public float capturingForce = 1000f;

		[KSPField(isPersistant = false), SerializeField]
		public float captureBreakingForceFactor = 0.3f;

		[KSPField(isPersistant = false), SerializeField]
		public float captureBreakingDistance = 0.15f;

		[KSPField(isPersistant = false), SerializeField]
		public float captureBreakingAngle = 5f;

		[KSPField(isPersistant = false), SerializeField]
		public float capturingSpeedTranslation = 0.025f; // distance per second


		[KSPField(isPersistant = false), SerializeField]
		public float latchingForce = 100000f;

		[KSPField(isPersistant = false), SerializeField]
		public float latchingBreakingForceFactor = 0.6f;

		[KSPField(isPersistant = false), SerializeField]
		public float latchingBreakingDistance = 0.06f;

		[KSPField(isPersistant = false), SerializeField]
		public float latchingBreakingAngle = 1.0f;

		[KSPField(isPersistant = false), SerializeField]
		public float latchingSpeedRotation = 0.1f; // degrees per second

		[KSPField(isPersistant = false), SerializeField]
		public float latchingSpeedTranslation = 0.025f; // distance per second


		[KSPField(isPersistant = false), SerializeField]
		public float latchingDistance = 0.002f;

		[KSPField(isPersistant = false), SerializeField]
		public float latchingAlignment = 0.04f;

		[KSPField(isPersistant = false), SerializeField]
		public float latchingAngle = 0.04f;


		[KSPField(isPersistant = false), SerializeField]
		public bool canCrossfeed = true;

		[KSPField(isPersistant = true)]
		public bool crossfeed = true;


		[KSPField(isPersistant = false), SerializeField]
		private float electricChargeRequiredLatching = 1.5f;

		[KSPField(isPersistant = false), SerializeField]
		private float electricChargeRequiredReleasing = 0.5f;

		private PartResourceDefinition electricResource = null;


		[KSPField(guiFormat = "S", guiActive = true, guiActiveEditor = true, guiName = "Port Name")]
		public string portName = "";


		public struct LookAtInfo
		{
			public string partName;
			public string targetName;
			public Vector3 direction;
			public bool stretch;
		};

		public List<LookAtInfo> aLookAtInfo;

		public struct LookAt
		{
			public Transform part;
			public Transform target;
			public Vector3 direction;
			public bool stretch;
			public float factor;
		};

		public List<LookAt> aLookAt;

		// Docking and Status

		public BaseEvent evtSetAsTarget;
		public BaseEvent evtUnsetTarget;

		public Transform nodeTransform;
		private GameObject controlObject;
		public Transform controlTransform;

		public KerbalFSM fsm;

		public KFSMState st_ready;			// "passive"

		public KFSMState st_extending;		// "activating"
		public KFSMState st_retracting;		// "deactivating"
		public KFSMState st_extended;		// "active" / "searching"
		
		public KFSMState st_approaching;	// port found
		public KFSMState st_approaching_passive;

		public KFSMState st_push;			// try to push the ring into the other port
		public KFSMState st_restore;		// pull the ring back from the other port (opposite of st_push)
		
		public KFSMState st_captured;		// the rings have a first connection
		public KFSMState st_captured_passive;
		
		public KFSMState st_latched;		// the rings have a stable connection and the system is ready for orienting, pullback and docking
	//	public KFSMState st_latched_passive;// not used -> passive port remains in st_captured_passive

		public KFSMState st_released;		// after a capture or latch, the rings have been detached again -> maybe for an abort of the docking
		
		public KFSMState st_preparedocking;	// orienting and retracting in progress
		public KFSMState st_predocked;		// ready to dock (the real docking process that makes 1 ship out of the 2)
		
		public KFSMState st_docked;
		public KFSMState st_preattached;

		public KFSMState st_disabled;


		public KFSMEvent on_extend;
		public KFSMEvent on_retract;

		public KFSMEvent on_extended;
		public KFSMEvent on_retracted;

		public KFSMEvent on_approach;
		public KFSMEvent on_distance;

		public KFSMEvent on_approach_passive;
		public KFSMEvent on_distance_passive;

		public KFSMEvent on_push;
		public KFSMEvent on_restore;

		public KFSMEvent on_capture;
		public KFSMEvent on_capture_passive;

		public KFSMEvent on_latch;
	//	public KFSMEvent on_latch_passive; // not used -> passive port remains in st_captured_passive

		public KFSMEvent on_release;
		public KFSMEvent on_release_passive;

		public KFSMEvent on_preparedocking;
		public KFSMEvent on_predock;

		public KFSMEvent on_dock;
		public KFSMEvent on_undock;

		public KFSMEvent on_enable;
		public KFSMEvent on_disable;

		public KFSMEvent on_construction;

		// Sounds

			// option for later

		// Ring

		private Transform ringTransform = null;

		private Transform ringTransformOrgParent;
		private Vector3 ringOrgLocalPosition;
		private Quaternion ringOrgLocalRotation;

		private GameObject ringObject;
		private ConfigurableJoint ringJoint;

		private Vector3 extendDirection;
		private float extendPosition = 0f;

		private float _pushStep = 0f;

		private float lastPreLatchDistance;

		private int iCapturePosition;

		// Capturing / Docking

		public ModuleDockingPortEx otherPort;
		public uint dockedPartUId;

		public DockedVesselInfo vesselInfo;

		private ConfigurableJoint joint;

		private Vector3 jointInitialPosition;

		private float jointBreakForce;
		private float jointBreakTorque;

		private Quaternion jointTargetRotation;
		private Vector3 jointTargetPosition;

		private float jointLastDistance;
		private float jointLastAlignment;

		private float progress;
private float _rotStep; private float _transStep; // FEHLER FEHLER, -> progress ist hier auf 2 aufgeteilt... mal prüfen was das soll oder ob wir's so lassen

		private float progressStep = 0.0005f;

		private int waitCounter;
		private int relaxCounter;

		private DockingPortStatus _state = null;

		// Packed / OnRails

		private Vector3 ringRelativePosition;
		private Quaternion ringRelativeRotation;

		private int followOtherPort = 0;

		////////////////////////////////////////
		// Constructor

		public ModuleDockingPortEx()
		{
		}

		////////////////////////////////////////
		// Callbacks

		public override void OnAwake()
		{
#if DEBUG
	//		DebugInit();
#endif

			part.dockingPorts.AddUnique(this);


			electricResource = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

			if(consumedResources == null)
				consumedResources = new List<PartResourceDefinition>();
			else
				consumedResources.Clear();

			consumedResources.Add(electricResource);
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			LoadLookAt(node);

			if(node.HasValue("portName"))
				portName = node.GetValue("portName");

			if(node.HasValue("state"))
				DockStatus = node.GetValue("state");
			else
				DockStatus = "Ready";

			if(node.HasValue("dockUId"))
				dockedPartUId = uint.Parse(node.GetValue("dockUId"));

			if(node.HasNode("DOCKEDVESSEL"))
			{
				vesselInfo = new DockedVesselInfo();
				vesselInfo.Load(node.GetNode("DOCKEDVESSEL"));
			}

			if(node.HasNode("PORTSTATUS"))
			{
				_state = new DockingPortStatus();
				_state.Load(node.GetNode("PORTSTATUS"));
			}
		}

		public DockingPortStatus BuildState()
		{
			DockingPortStatus state = new DockingPortStatus();

			state.extendPosition = extendPosition;

			if(ringJoint)
			{
				state.activeJointTargetPosition = ringJoint.targetPosition;
				state.activeJointTargetRotation = ringJoint.targetRotation;
			}

			state._pushStep = _pushStep;

			return state;
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

			node.AddValue("portName", portName);

			node.AddValue("state", (string)(((fsm != null) && (fsm.Started)) ? fsm.currentStateName : DockStatus));

			node.AddValue("dockUId", dockedPartUId);

			if(vesselInfo != null)
				vesselInfo.Save(node.AddNode("DOCKEDVESSEL"));

			BuildState().Save(node.AddNode("PORTSTATUS"));
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			nodeTypesAcceptedS = new HashSet<string>();

			string[] values = nodeTypesAccepted.Split(new char[2] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			foreach(string s in values)
				nodeTypesAcceptedS.Add(s);

			if(portName == string.Empty)
				portName = part.partInfo.title;

			Fields["autoFreeDriftMode"].OnValueModified += onChanged_autoFreeDriftMode;
			onChanged_autoFreeDriftMode(null);

			if(state == StartState.Editor)
			{
				EditorInitialize();
				return;
			}

			evtSetAsTarget = Events["SetAsTarget"];
			evtUnsetTarget = Events["UnsetTarget"];

			GameEvents.onVesselGoOnRails.Add(OnPack);
			GameEvents.onVesselGoOffRails.Add(OnUnpack);

		//	GameEvents.onFloatingOriginShift.Add(OnFloatingOriginShift);

			GameEvents.OnEVAConstructionModePartDetached.Add(OnEVAConstructionModePartDetached);

			nodeTransform = part.FindModelTransform(nodeTransformName);
			if(!nodeTransform)
			{
				Logger.Log("No node transform found with name " + nodeTransformName, Logger.Level.Error);
				return;
			}

			if(controlTransformName == string.Empty)
				controlTransform = part.transform;
			else
			{
				controlTransform = part.FindModelTransform(controlTransformName);
				if(!controlTransform)
				{
					Logger.Log("No control transform found with name " + controlTransformName, Logger.Level.Warning);
					controlTransform = part.transform;
				}
			}

			if(controlOrientation != Vector3.up)
			{
				controlObject = new GameObject();
				controlObject.transform.parent = controlTransform.transform;

				controlObject.transform.localPosition = Vector3.zero;
				controlObject.transform.localRotation = Quaternion.identity;

				controlObject.transform.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(controlOrientation, Vector3.up, Vector3.forward), nodeTransform.rotation * Vector3.forward) * controlObject.transform.rotation;

				controlTransform = controlObject.transform;
			}

			StartCoroutine(WaitAndInitialize(state));

	//		StartCoroutine(WaitAndDisableDockingNode());
		}

		public IEnumerator WaitAndInitialize(StartState st)
		{
			yield return null;

			InitializeMeshes();
			InitializeLookAt();

			Events["TogglePort"].active = false;

			Events["ExtendRing"].active = false;
			Events["RetractRing"].active = false;

			Events["Release"].active = false;
			Events["PerformDocking"].active = false;

			Events["Undock"].active = false;

			ResetDockInfo();

			if(!canCrossfeed) crossfeed = false;

			part.fuelCrossFeed = crossfeed;

			Events["EnableXFeed"].active = !crossfeed;
			Events["DisableXFeed"].active = crossfeed;

			if(dockedPartUId != 0)
			{
				Part otherPart;

				while(!(otherPart = FlightGlobals.FindPartByID(dockedPartUId)))
					yield return null;

				otherPort = otherPart.GetComponent<ModuleDockingPortEx>();

				while(otherPort.ringTransform == null)
					yield return null;

				// other port will load this too -> we set this just in case something does not work
				otherPort.otherPort = this;
				otherPort.dockedPartUId = part.flightID;
			}

			if((DockStatus == "Inactive")
			|| ((DockStatus == "Attached") && (otherPort == null)))
			{
				if(referenceAttachNode != string.Empty)
				{
					AttachNode node = part.FindAttachNode(referenceAttachNode);
					if((node != null) && node.attachedPart)
					{
						ModuleDockingPortEx _otherPort = node.attachedPart.GetComponent<ModuleDockingPortEx>();

						if(_otherPort)
						{
							otherPort = _otherPort;
							dockedPartUId = otherPort.part.flightID;

							DockStatus = "Attached";
							otherPort.DockStatus = "Attached";
						}
					}
				}
			}

			SetupFSM();

			if(DockStatus == "Retracting ring")
				DockStatus = "Ready";

			if((DockStatus == "Push ring")
			|| (DockStatus == "Restore ring"))
				DockStatus = "Approaching";

			if(DockStatus == "Capture released")
				DockStatus = "Searching";

			if(DockStatus == "Docking")
				DockStatus = "Retracting ring";

			if((DockStatus == "Extending ring")
			|| (DockStatus == "Searching")
			|| (DockStatus == "Approaching")
			|| (DockStatus == "Captured")
			|| (DockStatus == "Latched")
			|| (DockStatus == "Retracting ring"))
			{
				BuildRingObject();
				ringJoint = BuildRingJoint();

				extendPosition = _state.extendPosition;

				ringJoint.targetPosition = _state.activeJointTargetPosition;
				ringJoint.targetRotation = _state.activeJointTargetRotation;

				_pushStep = _state._pushStep;

				// Pack

				ringObject.GetComponent<Rigidbody>().isKinematic = true;
				ringObject.GetComponent<Rigidbody>().detectCollisions = false;

				ringObject.transform.parent = transform;
			}

			if((DockStatus == "Captured")
			|| (DockStatus == "Latched")
			|| (DockStatus == "Retracting ring"))
			{
				BuildJoint();
				CalculateJointTarget();

				relaxCounter = 8;
			}

			if((DockStatus == "Latched")
			|| (DockStatus == "Retracting ring"))
			{
				ringObject.transform.localPosition = _capturePositionB;
				ringObject.transform.localRotation = _captureRotationB;

				iCapturePosition = 25;

				float f, d;

				f = 10000f * iCapturePosition;
				d = 0.001f;

				JointDrive drive = new JointDrive
				{
					positionSpring = f,
					positionDamper = d,
					maximumForce = f
				};

				joint.angularXDrive = joint.angularYZDrive = drive;
				joint.xDrive = joint.yDrive = joint.zDrive = drive;
			}

			if(DockStatus == "Docked")
			{
				DockingHelper.OnLoad(this, vesselInfo, otherPort, otherPort.vesselInfo);
			}

			if((DockStatus == "Approaching")
			|| (DockStatus == "Captured")
			|| (DockStatus == "Latched")
			|| (DockStatus == "Retracting ring"))
			{
				if(otherPort != null)
				{
					while((otherPort.fsm == null) || (!otherPort.fsm.Started))
						yield return null;
				}
			}

			fsm.StartFSM(DockStatus);

			if(joint || (otherPort && otherPort.joint))
			{
				if(Vessel.GetDominantVessel(vessel, otherPort.vessel) == otherPort.vessel)
					followOtherPort = VesselPositionManager.Register(part, otherPort.part);
			}
		}
	/*
		public IEnumerator WaitAndDisableDockingNode()
		{
			ModuleDockingNode DockingNode = part.FindModuleImplementing<ModuleDockingNode>();

			if(DockingNode)
			{
				while((DockingNode.fsm == null) || (!DockingNode.fsm.Started))
					yield return null;

				DockingNode.fsm.RunEvent(DockingNode.on_disable);
			}
		}
	*/
		public void OnDestroy()
		{
			if(controlObject != null)
				Destroy(controlObject);

			if(ringObject != null)
				Destroy(ringObject);

			Fields["autoFreeDriftMode"].OnValueModified -= onChanged_autoFreeDriftMode;

			GameEvents.onVesselGoOnRails.Remove(OnPack);
			GameEvents.onVesselGoOffRails.Remove(OnUnpack);

		//	GameEvents.onFloatingOriginShift.Remove(OnFloatingOriginShift);

			GameEvents.OnEVAConstructionModePartDetached.Remove(OnEVAConstructionModePartDetached);

			if(HighLogic.LoadedSceneIsEditor)
				GameEvents.onEditorPartEvent.Remove(OnEditorPartEvent);
		}

		private void OnPack(Vessel v)
		{
			if(vessel == v)
			{
				if((DockStatus == "Extending ring")
				|| (DockStatus == "Retracting ring")
				|| (DockStatus == "Searching")
				|| (DockStatus == "Approaching")
				|| (DockStatus == "Push ring")
				|| (DockStatus == "Restore ring")
				|| (DockStatus == "Capture released"))
				{
					ringObject.GetComponent<Rigidbody>().isKinematic = true;
					ringObject.GetComponent<Rigidbody>().detectCollisions = false;

					ringObject.transform.parent = transform;
				}

				if((DockStatus == "Captured")
				|| (DockStatus == "Latched"))
				{
					ringRelativePosition = ringObject.transform.localPosition;
					ringRelativeRotation = ringObject.transform.localRotation;

					ringObject.transform.parent = transform;
				}

				if(joint || (otherPort && otherPort.joint))
				{
					if(Vessel.GetDominantVessel(vessel, otherPort.vessel) == otherPort.vessel)
						followOtherPort = VesselPositionManager.Register(part, otherPort.part);
				}
			}
		}

		private void OnUnpack(Vessel v)
		{
			if(vessel == v)
			{
				if(followOtherPort != 0)
				{
					VesselPositionManager.Unregister(followOtherPort);
					followOtherPort = 0;
				}

				StartCoroutine(OnUnpackDelayed());
			}
		}

			// FEHLER, ich denke, das muss sein, könnte aber sein, dass es auch ohne ginge
		public IEnumerator OnUnpackDelayed()
		{
			for(int i = 0; i < 25; i++)
				yield return new WaitForFixedUpdate();

			if((DockStatus == "Extending ring")
			|| (DockStatus == "Retracting ring")
			|| (DockStatus == "Searching")
			|| (DockStatus == "Approaching")
			|| (DockStatus == "Push ring")
			|| (DockStatus == "Restore ring")
			|| (DockStatus == "Capture released"))
			{
				ringObject.GetComponent<Rigidbody>().isKinematic = false;
				ringObject.GetComponent<Rigidbody>().detectCollisions = true;

				ringObject.transform.parent = null;
			}

			if((DockStatus == "Captured")
			|| (DockStatus == "Latched"))
			{
				ringObject.transform.parent = otherPort.transform;

				ringObject.transform.localPosition = ringRelativePosition;
				ringObject.transform.localRotation = ringRelativeRotation;
			}
		}

	//	private void OnFloatingOriginShift(Vector3d offset, Vector3d nonFrame) -> do something ?

		private void OnEVAConstructionModePartDetached(Vessel v, Part p)
		{
			if(part == p)
			{
				if(otherPort)
				{
					otherPort.otherPort = null;
					otherPort.dockedPartUId = 0;
					otherPort.fsm.RunEvent(otherPort.on_construction);
				}

				otherPort = null;
				dockedPartUId = 0;
				fsm.RunEvent(on_construction);
			}
		}

		////////////////////////////////////////
		// Functions

		private void InitializeMeshes()
		{
			if(ringTransform != null)
				return;

			ringTransform = part.FindModelTransform(ringName);

			ringTransformOrgParent = ringTransform.parent;
			ringOrgLocalPosition = ringTransform.localPosition;
			ringOrgLocalRotation = ringTransform.localRotation;
		}

		private void LoadLookAt(ConfigNode node)
		{
			if(aLookAtInfo == null)
			{
				if((part.partInfo == null) || (part.partInfo.partPrefab == null))
				{
					// I assume, that I'm the prefab then

					aLookAtInfo = new List<LookAtInfo>();

					ConfigNode[] lookatnodes = node.GetNodes("LOOKAT");
					for(int i = 0; i < lookatnodes.Length; i++)
					{
						ConfigNode lookatnode = lookatnodes[i];

						LookAtInfo info = new LookAtInfo();

						lookatnode.TryGetValue("part", ref info.partName);
						lookatnode.TryGetValue("target", ref info.targetName);
						if(!lookatnode.TryGetValue("direction", ref info.direction))
							info.direction = Vector3.forward;
						if(!lookatnode.TryGetValue("stretch", ref info.stretch))
							info.stretch = false;

						aLookAtInfo.Add(info);
					}
				}
			}
		}

		private void InitializeLookAt()
		{
			if(aLookAtInfo == null)
			{
				if((part.partInfo != null) && (part.partInfo.partPrefab != null))
				{
					ModuleDockingPortEx prefabModule = (ModuleDockingPortEx)part.partInfo.partPrefab.Modules["ModuleDockingPortEx"];
					if(prefabModule != null)
					{
						aLookAtInfo = prefabModule.aLookAtInfo;

						aLookAt = new List<LookAt>(aLookAtInfo.Count);

						for(int i = 0; i < aLookAtInfo.Count; i++)
						{
							LookAtInfo info = aLookAtInfo[i];

							LookAt l = new LookAt();

							l.part = part.FindModelTransform(info.partName);
							l.target = part.FindModelTransform(info.targetName);

							l.direction = info.direction;
							l.stretch = info.stretch;
							if(l.stretch)
								l.factor = l.part.localScale.y / (l.target.position - l.part.position).magnitude;

							aLookAt.Add(l);
						}
					}
				}
			}

			if(aLookAt == null)
				aLookAt = new List<LookAt>();

			UpdatePistons();
		}

		public void SetupFSM()
		{
			fsm = new KerbalFSM();

			st_ready = new KFSMState("Ready");
			st_ready.OnEnter = delegate(KFSMState from)
			{
				otherPort = null;
				dockedPartUId = 0;

				Events["TogglePort"].guiName = "Deactivate Port";
				Events["TogglePort"].active = true;

				Events["ExtendRing"].active = ((supportedModes & 2) != 0);

				Events["ToggleAutoFreeDriftMode"].active = ((supportedModes & 2) != 0);

				DockStatus = st_ready.name;
			};
			st_ready.OnFixedUpdate = delegate
			{
			};
			st_ready.OnLeave = delegate(KFSMState to)
			{
				if(to != st_disabled)
					Events["TogglePort"].active = false;

				Events["ExtendRing"].active = false;
			};
			fsm.AddState(st_ready);

			st_extending = new KFSMState("Extending ring");
			st_extending.OnEnter = delegate(KFSMState from)
			{
				if(ringObject == null)
					BuildRingObject();

				if(ringJoint == null)
					ringJoint = BuildRingJoint();

				Events["RetractRing"].active = true;

				DockStatus = st_extending.name;
			};
			st_extending.OnFixedUpdate = delegate
			{
				if(extendPosition < extensionLength)
				{
					extendPosition = Mathf.Min(extensionLength, extendPosition + extensionSpeed);

					ringJoint.targetPosition = extendDirection * (extendPosition - (maxExtensionLength * 0.5f));
				}
				else
					fsm.RunEvent(on_extended);
			};
			st_extending.OnLeave = delegate(KFSMState to)
			{
				if(to != st_extended)
					Events["RetractRing"].active = false;
			};
			fsm.AddState(st_extending);

			st_retracting = new KFSMState("Retracting ring");
			st_retracting.OnEnter = delegate(KFSMState from)
			{
				otherPort = null;
				dockedPartUId = 0;

				Events["ExtendRing"].active = true;

				DockStatus = st_retracting.name;
			};
			st_retracting.OnFixedUpdate = delegate
			{
				if(extendPosition > 0f)
				{
					extendPosition = Mathf.Max(0f, extendPosition - extensionSpeed);

					ringJoint.targetPosition = extendDirection * (extendPosition - (maxExtensionLength * 0.5f));
				}
				else
					fsm.RunEvent(on_retracted);
			};
			st_retracting.OnLeave = delegate(KFSMState to)
			{
				if(to != st_extending)
					Events["ExtendRing"].active = false;

				if(to != st_ready)
					return;

				Destroy(ringJoint);
				ringJoint = null;

				DestroyRingObject();
				ringObject = null;

				UpdatePistons();
			};
			fsm.AddState(st_retracting);

			st_extended = new KFSMState("Searching");
			st_extended.OnEnter = delegate(KFSMState from)
			{
				otherPort = null;
				dockedPartUId = 0;

				Events["RetractRing"].active = true;

				_pushStep = 0f;

				DockStatus = st_extended.name;
			};
			st_extended.OnFixedUpdate = delegate
			{
				float relevantDistance; float alignment;

				for(int i = 0; i < FlightGlobals.VesselsLoaded.Count; i++)
				{
					Vessel vessel = FlightGlobals.VesselsLoaded[i];

					if(vessel.packed
						/*|| (vessel == part.vessel)*/) // no docking to ourself is possible
						continue;

					for(int j = 0; j < vessel.dockingPorts.Count; j++)
					{
						PartModule partModule = vessel.dockingPorts[j];

						if((partModule.part == null)
						/*|| (partModule.part == part)*/ // no docking to ourself is possible
						|| (partModule.part.State == PartStates.DEAD))
							continue;

						ModuleDockingPortEx _otherPort = partModule.GetComponent<ModuleDockingPortEx>();

						if(_otherPort == null)
							continue;

						if(!nodeTypesAcceptedS.Contains(_otherPort.nodeType)
						|| !_otherPort.nodeTypesAcceptedS.Contains(nodeType))
							continue;

						if(_otherPort.fsm.CurrentState != _otherPort.st_ready)
							continue;

						relevantDistance = (_otherPort.ringTransform.transform.position - ringObject.transform.position).magnitude;

						if(relevantDistance < detectionDistance)
						{
							DockDistance = relevantDistance.ToString("F4");

							alignment = Vector3.Angle(nodeTransform.forward, -_otherPort.nodeTransform.forward);

							if((alignment <= approachingAlignment) && (relevantDistance <= approachingDistance))
							{
								DockAlignment = alignment.ToString("F3") + "°";
								DockAngle = "-";

								// we don't expect to see multiple matching ports in the same area
								// that's why we don't continue to search and simply take the first we find

								otherPort = _otherPort;
								dockedPartUId = otherPort.part.flightID;

								fsm.RunEvent(on_approach);
								otherPort.fsm.RunEvent(otherPort.on_approach_passive);
								return;
							}
						}
					}
				}

				ResetDockInfo();
			};
			st_extended.OnLeave = delegate(KFSMState to)
			{
				if(to != st_approaching)
					Events["RetractRing"].active = false;

				if(to != st_approaching)
					ResetDockInfo();
			};
			fsm.AddState(st_extended);

			st_approaching = new KFSMState("Approaching");
			st_approaching.OnEnter = delegate(KFSMState from)
			{
				Events["RetractRing"].active = true;

				otherPort.otherPort = this;
				otherPort.dockedPartUId = part.flightID;

			//	otherPort.fsm.RunEvent(otherPort.on_approach_passive); -> is done manually

				_pushStep = 0f;

				DockStatus = st_approaching.name;
			};
			st_approaching.OnFixedUpdate = delegate
			{
				float relevantDistance = (otherPort.ringTransform.transform.position - ringObject.transform.position).magnitude - correctionVector.magnitude;
					// FEHLER, relevantAlignment? relevantAngle?

			//	float distance = (otherPort.nodeTransform.position - nodeTransform.position).magnitude;
				float alignment = Vector3.Angle(nodeTransform.forward, -otherPort.nodeTransform.forward);
				float angle = CalculateAngle();

				DockDistance = relevantDistance.ToString("F4");
				DockAlignment = alignment.ToString("F3") + "°";
				DockAngle = angle.ToString("F3") + "°";

				if(relevantDistance < (maxExtensionLength - extensionLength))
					fsm.RunEvent(on_push);
				else
				{
					if(relevantDistance < 1.5f * approachingDistance)
					{
						if(alignment <= approachingAlignment)
							return;
					}

					otherPort.fsm.RunEvent(otherPort.on_distance_passive);
					fsm.RunEvent(on_distance);
				}
			};
			st_approaching.OnLeave = delegate(KFSMState to)
			{
				if((to != st_extended) && (to != st_push))
					Events["RetractRing"].active = false;
			};
			fsm.AddState(st_approaching);

			st_approaching_passive = new KFSMState("Approached");
			st_approaching_passive.OnEnter = delegate(KFSMState from)
			{
				DockStatus = st_approaching_passive.name;
			};
			st_approaching_passive.OnFixedUpdate = delegate
			{
			};
			st_approaching_passive.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_approaching_passive);

			st_push = new KFSMState("Push ring");
			st_push.OnEnter = delegate(KFSMState from)
			{
				Events["RetractRing"].active = true;

				DockStatus = st_push.name;
			};
			st_push.OnFixedUpdate = delegate
			{
				float relevantDistance = (otherPort.ringTransform.transform.position - ringObject.transform.position).magnitude - correctionVector.magnitude;

			//	float distance = (otherPort.nodeTransform.position - nodeTransform.position).magnitude;
				float alignment = Vector3.Angle(nodeTransform.forward, -otherPort.nodeTransform.forward);
				float angle = CalculateAngle();

				DockDistance = relevantDistance.ToString("F4");
				DockAlignment = alignment.ToString("F3") + "°";
				DockAngle = angle.ToString("F3") + "°";

// FEHLER, 's gibt noch 'n captureAngle... warum ist uns der egal hier???
				if(relevantDistance <= captureDistance)
				{
					fsm.RunEvent(on_capture);
					otherPort.fsm.RunEvent(otherPort.on_capture_passive);

				}
				else if(relevantDistance > (maxExtensionLength - extensionLength) * 1.4f)
					fsm.RunEvent(on_restore);
				else
				{
					Quaternion ActiveJointTargetRotation; Vector3 ActiveJointTargetPosition;

					CalculateActiveJointRotationAndPosition(otherPort, out ActiveJointTargetRotation, out ActiveJointTargetPosition);

					_pushStep = Mathf.Min(1.0f, _pushStep + pushSpeed);

					ringJoint.targetRotation = Quaternion.Slerp(Quaternion.identity, ActiveJointTargetRotation, _pushStep);
					ringJoint.targetPosition = Vector3.Lerp(extendDirection * (extendPosition - (maxExtensionLength * 0.5f)), ActiveJointTargetPosition, _pushStep);
				}
			};
			st_push.OnLeave = delegate(KFSMState to)
			{
				if(to != st_restore)
					Events["RetractRing"].active = false;
			};
			fsm.AddState(st_push);

			st_restore = new KFSMState("Restore ring");
			st_restore.OnEnter = delegate(KFSMState from)
			{
				Events["RetractRing"].active = true;

				DockStatus = st_restore.name;
			};
			st_restore.OnFixedUpdate = delegate
			{
				float relevantDistance = (otherPort.ringTransform.transform.position - ringObject.transform.position).magnitude - correctionVector.magnitude;

			//	float distance = (otherPort.nodeTransform.position - nodeTransform.position).magnitude;
				float alignment = Vector3.Angle(nodeTransform.forward, -otherPort.nodeTransform.forward);
				float angle = CalculateAngle();

				DockDistance = relevantDistance.ToString("F4");
				DockAlignment = alignment.ToString("F3") + "°";
				DockAngle = angle.ToString("F3") + "°";

				if(relevantDistance < (maxExtensionLength - extensionLength))
					fsm.RunEvent(on_push);
				else
				{
					Quaternion ActiveJointTargetRotation; Vector3 ActiveJointTargetPosition;

					CalculateActiveJointRotationAndPosition(otherPort, out ActiveJointTargetRotation, out ActiveJointTargetPosition);

					_pushStep = Mathf.Max(0f, _pushStep - pushSpeed);

					if(_pushStep > 0f)
					{
						ringJoint.targetRotation = Quaternion.Slerp(Quaternion.identity, ActiveJointTargetRotation, _pushStep);
						ringJoint.targetPosition = Vector3.Lerp(extendDirection * (extendPosition - (maxExtensionLength * 0.5f)), ActiveJointTargetPosition, _pushStep);
					}
					else
					{
						_pushStep = 0f;

						ringJoint.targetRotation = Quaternion.identity;
						ringJoint.targetPosition = extendDirection * (extendPosition - (maxExtensionLength * 0.5f));

						fsm.RunEvent(on_approach);
					}
				}
			};
			st_restore.OnLeave = delegate(KFSMState to)
			{
				if(to != st_approaching)
					Events["RetractRing"].active = false;
			};
			fsm.AddState(st_restore);
		
			st_captured = new KFSMState("Captured");
			st_captured.OnEnter = delegate(KFSMState from)
			{
				Events["Release"].active = true;

				BuildJoint();
				CalculateJointTarget();

				lastPreLatchDistance = (otherPort.nodeTransform.position - nodeTransform.position).magnitude;

				switch(autoFreeDriftMode)
				{
				case 0:
					break;
				case 1:
					part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
					break;
				case 2:
					part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
					part.vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, false);
					break;
				}

				relaxCounter = 8;

				DockStatus = st_captured.name;
			};
			st_captured.OnFixedUpdate = delegate
			{
				// OPTION: add possibility for failure when damping movements/aligning is not possible after applying force (if no progress is detected)

				if(_captureSlerp < 1f)
				{
					_captureSlerp = Math.Min(1f, _captureSlerp + 0.05f);

					ringObject.transform.localPosition =
						Vector3.Lerp(_capturePositionA, _capturePositionB, _captureSlerp);

					ringObject.transform.localRotation =
						Quaternion.Slerp(_captureRotationA, _captureRotationB, _captureSlerp);

					return;
				}

				if(--relaxCounter > 0)
					return;

				++iCapturePosition;
				relaxCounter = 8;

				{
					float f, d;

					if(iCapturePosition < 0)
					{
						f = Mathf.Max((iCapturePosition + 50) * 50f, 100f);
						d = 0.002f;

						float preLatchDistance = (otherPort.nodeTransform.position - nodeTransform.position).magnitude;

						if(Mathf.Abs(preLatchDistance - lastPreLatchDistance) < 0.001f)
							relaxCounter = 1;
						else
							lastPreLatchDistance = (2f * lastPreLatchDistance + preLatchDistance) / 3f;
					}
					else
					{
						f = 10000f * iCapturePosition;
						d = 0.001f;
					}

					JointDrive drive = new JointDrive
					{
						positionSpring = f,
						positionDamper = d,
						maximumForce = f
					};

					joint.angularXDrive = joint.angularYZDrive = drive;
					joint.xDrive = joint.yDrive = joint.zDrive = drive;

					if(iCapturePosition >= 25)
					{
						fsm.RunEvent(on_latch);
					}
				}
			};
			st_captured.OnLeave = delegate(KFSMState to)
			{
				if(to != st_latched)
					Events["Release"].active = false;
			};
			fsm.AddState(st_captured);

			st_captured_passive = new KFSMState("Target");
			st_captured_passive.OnEnter = delegate(KFSMState from)
			{
				DockStatus = st_captured_passive.name;
			};
			st_captured_passive.OnFixedUpdate = delegate
			{
			};
			st_captured_passive.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_captured_passive);

			st_released = new KFSMState("Capture released");
			st_released.OnEnter = delegate(KFSMState from)
			{
				DestroyJoint();

				Events["RetractRing"].active = true;

				DockStatus = st_released.name;
			};
			st_released.OnFixedUpdate = delegate
			{
				if(_pushStep > 0f)
				{
					Quaternion ActiveJointTargetRotation; Vector3 ActiveJointTargetPosition;

					CalculateActiveJointRotationAndPosition(otherPort, out ActiveJointTargetRotation, out ActiveJointTargetPosition);

					_pushStep = Mathf.Max(0f, _pushStep - pushSpeed);

					if(_pushStep > 0f)
					{
						ringJoint.targetRotation = Quaternion.Slerp(Quaternion.identity, ActiveJointTargetRotation, _pushStep);
						ringJoint.targetPosition = Vector3.Lerp(extendDirection * (extendPosition - (maxExtensionLength * 0.5f)), ActiveJointTargetPosition, _pushStep);
					}
					else
					{
						_pushStep = 0f;

						ringJoint.targetRotation = Quaternion.identity;
						ringJoint.targetPosition = extendDirection * (extendPosition - (maxExtensionLength * 0.5f));
					}
				}
				else
				{
					float relevantDistance = (otherPort.ringTransform.transform.position - ringObject.transform.position).magnitude - correctionVector.magnitude;

					DockDistance = relevantDistance.ToString("F4");
					DockAlignment = "-";
					DockAngle = "-";

					if(relevantDistance > (maxExtensionLength - extensionLength) * 1.4f)
					{
						otherPort = null;
						dockedPartUId = 0;

						fsm.RunEvent(on_extended);
					}
				}
			};
			st_released.OnLeave = delegate(KFSMState to)
			{
				ResetDockInfo();
			};
			fsm.AddState(st_released);
		
			st_latched = new KFSMState("Latched");
			st_latched.OnEnter = delegate(KFSMState from)
			{
				Events["Release"].active = true;
				Events["PerformDocking"].active = true;

				DockStatus = st_latched.name;
			};
			st_latched.OnFixedUpdate = delegate
			{
			};
			st_latched.OnLeave = delegate(KFSMState to)
			{
				Events["Release"].active = false;
				Events["PerformDocking"].active = false;
			};
			fsm.AddState(st_latched);
		
			st_preparedocking = new KFSMState("Retracting ring");
			st_preparedocking.OnEnter = delegate(KFSMState from)
			{
				// OPTION: add abort docking option?

				Destroy(ringJoint);
				ringJoint = null;

				_rotStep = 1f;
				_transStep = 1f;

				DockStatus = st_preparedocking.name;
			};
			st_preparedocking.OnFixedUpdate = delegate
			{
				// OPTION: add possibility for failure when alignment/positioning is not possible after applying force (if no progress is detected)

				if((_rotStep > 0.01f) || (_transStep > 0.01f))
				{
			//		DrawRelative(2, otherPort.DockingNode.nodeTransform.position, otherPort.DockingNode.nodeTransform.forward);
			//		DrawRelative(4, otherPort.DockingNode.nodeTransform.position, otherPort.DockingNode.nodeTransform.up);

			//		DrawRelative(6, DockingNode.nodeTransform.position, DockingNode.nodeTransform.forward);
			//		DrawRelative(8, DockingNode.nodeTransform.position, DockingNode.nodeTransform.up);

					DockStatus = "docking (orientation)";

					_rotStep -= 1f / (Quaternion.Angle(Quaternion.identity, jointTargetRotation) / 0.008f);
					if(_rotStep < 0) _rotStep = 0f;

					joint.targetRotation = Quaternion.Slerp(jointTargetRotation, Quaternion.identity, _rotStep);

					// find distance from my axis
					Vector3 diff = otherPort.nodeTransform.position - nodeTransform.position;
					Vector3 diffp = Vector3.ProjectOnPlane(diff, nodeTransform.forward);

					Vector3 diffpl = /*CaptureJoint.transform.rotation **/ (Quaternion.Inverse(nodeTransform.rotation) * diffp);
					diffpl = Quaternion.Inverse(joint.transform.rotation) * diffp;

					if(diffpl.magnitude < 0.0005f)
					{
						joint.targetPosition -= diffpl;
						_transStep = 0f;
					}
					else
						joint.targetPosition -= diffpl.normalized * 0.0005f;
				}
				else
				{
					DockStatus = "docking (retracting)";

					joint.targetRotation = jointTargetRotation;

					Vector3 diff = otherPort.nodeTransform.position - nodeTransform.position;
					diff = joint.transform.InverseTransformDirection(diff);

					if(diff.magnitude < 0.0005f)
					{
						joint.targetPosition -= diff;

						fsm.RunEvent(on_predock);
					}
					else
						joint.targetPosition -= diff.normalized * 0.0005f;
				}
			};
			st_preparedocking.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_preparedocking);

			st_predocked = new KFSMState("Docking");
			st_predocked.OnEnter = delegate(KFSMState from)
			{
				relaxCounter = 10;

				DockStatus = st_predocked.name;
			};
			st_predocked.OnFixedUpdate = delegate
			{
				if(--relaxCounter < 0)
				{
					fsm.RunEvent(on_dock);
					otherPort.fsm.RunEvent(otherPort.on_dock);
				}
			};
			st_predocked.OnLeave = delegate(KFSMState to)
			{
				DestroyRingObject();
				ringObject = null;

				UpdatePistons();

				extendPosition = 0f;

				if(Vessel.GetDominantVessel(vessel, otherPort.vessel) == otherPort.vessel)
					DockToVessel(otherPort);
				else
					otherPort.DockToVessel(this);

				Destroy(joint);
				joint = null;
			};
			fsm.AddState(st_predocked);
		
			st_docked = new KFSMState("Docked");
			st_docked.OnEnter = delegate(KFSMState from)
			{
				Events["ToggleAutoFreeDriftMode"].active = false;

				Events["Undock"].active = true;

				DockStatus = st_docked.name;
			};
			st_docked.OnFixedUpdate = delegate
			{
			};
			st_docked.OnLeave = delegate(KFSMState to)
			{
				Events["Undock"].active = false;

				otherPort = null;
				dockedPartUId = 0;
			};
			fsm.AddState(st_docked);

			st_preattached = new KFSMState("Attached");
			st_preattached.OnEnter = delegate(KFSMState from)
			{
				Events["Undock"].active = true;

				DockStatus = st_preattached.name;
			};
			st_preattached.OnFixedUpdate = delegate
			{
			};
			st_preattached.OnLeave = delegate(KFSMState to)
			{
				Events["Undock"].active = false;

				otherPort = null;
				dockedPartUId = 0;
			};
			fsm.AddState(st_preattached);

			st_disabled = new KFSMState("Inactive");
			st_disabled.OnEnter = delegate(KFSMState from)
			{
				Events["TogglePort"].guiName = "Activate Port";
				Events["TogglePort"].active = true;

				Events["ToggleAutoFreeDriftMode"].active = false;

				ResetDockInfo();

				DockStatus = st_disabled.name;
			};
			st_disabled.OnFixedUpdate = delegate
			{
			};
			st_disabled.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_disabled);


			on_extend = new KFSMEvent("Extend Ring");
			on_extend.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_extend.GoToStateOnEvent = st_extending;
			fsm.AddEvent(on_extend, st_ready, st_retracting);

			on_retract = new KFSMEvent("Retract Ring");
			on_retract.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_retract.GoToStateOnEvent = st_retracting;
			fsm.AddEvent(on_retract, st_extending, st_extended, st_approaching, st_push);

			on_extended = new KFSMEvent("Ring extended");
			on_extended.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_extended.GoToStateOnEvent = st_extended;
			fsm.AddEvent(on_extended, st_extending, st_released);

			on_retracted = new KFSMEvent("Ring retracted");
			on_retracted.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_retracted.GoToStateOnEvent = st_ready;
			fsm.AddEvent(on_retracted, st_retracting);


			on_approach = new KFSMEvent("Approaching");
			on_approach.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_approach.GoToStateOnEvent = st_approaching;
			fsm.AddEvent(on_approach, st_extended, st_restore);

			on_distance = new KFSMEvent("Distancing");
			on_distance.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_distance.GoToStateOnEvent = st_extended;
			fsm.AddEvent(on_distance, st_approaching);

			on_approach_passive = new KFSMEvent("Approached");
			on_approach_passive.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_approach_passive.GoToStateOnEvent = st_approaching_passive;
			fsm.AddEvent(on_approach_passive, st_ready);

			on_distance_passive = new KFSMEvent("Distanced");
			on_distance_passive.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_distance_passive.GoToStateOnEvent = st_ready;
			fsm.AddEvent(on_distance_passive, st_approaching_passive);

			on_push = new KFSMEvent("Push Ring");
			on_push.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_push.GoToStateOnEvent = st_push;
			fsm.AddEvent(on_push, st_approaching);

			on_restore = new KFSMEvent("Restore Ring");
			on_restore.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_restore.GoToStateOnEvent = st_restore;
			fsm.AddEvent(on_restore, st_push);

			on_capture = new KFSMEvent("Capture");
			on_capture.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_capture.GoToStateOnEvent = st_captured;
			fsm.AddEvent(on_capture, st_push);

			on_release = new KFSMEvent("Release capture");
			on_release.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_release.GoToStateOnEvent = st_released;
			fsm.AddEvent(on_release, st_captured, st_latched);

			on_capture_passive = new KFSMEvent("Capture (as target)");
			on_capture_passive.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_capture_passive.GoToStateOnEvent = st_captured_passive;
			fsm.AddEvent(on_capture_passive, st_approaching_passive, st_ready);

			on_release_passive = new KFSMEvent("Release capture (as target)");
			on_release_passive.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_release_passive.GoToStateOnEvent = st_ready;
			fsm.AddEvent(on_release_passive, st_captured_passive);

			on_latch = new KFSMEvent("Latch");
			on_latch.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_latch.GoToStateOnEvent = st_latched;
			fsm.AddEvent(on_latch, st_captured);

			on_preparedocking = new KFSMEvent("Prepare docking");
			on_preparedocking.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_preparedocking.GoToStateOnEvent = st_preparedocking;
			fsm.AddEvent(on_preparedocking, st_latched);

			on_predock = new KFSMEvent("Ready for docking");
			on_predock.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_predock.GoToStateOnEvent = st_predocked;
			fsm.AddEvent(on_predock, st_preparedocking);

			on_dock = new KFSMEvent("Perform docking");
			on_dock.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_dock.GoToStateOnEvent = st_docked;
			fsm.AddEvent(on_dock, st_predocked, st_captured_passive);

			on_undock = new KFSMEvent("Undock");
			on_undock.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_undock.GoToStateOnEvent = st_ready;
			fsm.AddEvent(on_undock, st_docked, st_preattached);


			on_enable = new KFSMEvent("Enable");
			on_enable.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_enable.GoToStateOnEvent = st_ready;
			fsm.AddEvent(on_enable, st_disabled);

			on_disable = new KFSMEvent("Disable");
			on_disable.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_disable.GoToStateOnEvent = st_disabled;
			fsm.AddEvent(on_disable, st_ready);


			on_construction = new KFSMEvent("Construction");
			on_construction.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_construction.GoToStateOnEvent = st_disabled;
			fsm.AddEvent(on_construction, st_ready, st_extending, st_retracting, st_extended, st_approaching, st_approaching_passive, st_push, st_restore, st_captured, st_captured_passive, st_latched, st_released, st_preparedocking, st_predocked, st_docked, st_preattached);
		}

		private float CalculateAngle()
		{
			Vector3 tvref = nodeTransform.TransformDirection(dockingOrientation);
			Vector3 tv = otherPort.nodeTransform.TransformDirection(otherPort.dockingOrientation);
			float angle = Vector3.SignedAngle(tvref, tv, -nodeTransform.forward);

			angle = 360f + angle - (180f / snapCount);
			angle %= (360f / snapCount);
			angle -= (180f / snapCount);

			return angle;
		}
		
		private void ResetDockInfo()
		{
			DockDistance = "-";
			DockAlignment = "-";
			DockAngle = "-";
		}

		void BuildRingObject()
		{
			ringObject = new GameObject();

			ringObject.AddComponent<Rigidbody>().mass = 0.005f;

			ringObject.transform.position = ringTransform.transform.position;
			ringObject.transform.rotation = ringTransform.transform.rotation;

			ringTransform.parent = ringObject.transform;

			// latest time to initialize this value
			extendDirection = Quaternion.Inverse(transform.rotation) * nodeTransform.forward;
		}

		void DestroyRingObject()
		{
			ringTransform.parent = ringTransformOrgParent;
			ringTransform.localPosition = ringOrgLocalPosition;
			ringTransform.localRotation = ringOrgLocalRotation;

			Destroy(ringObject);
		}

		// calculate position and orientation for st_push / st_restore
		void CalculateActiveJointRotationAndPosition(ModuleDockingPortEx port, out Quaternion rotation, out Vector3 position)
		{
			Vector3 tvref =
				transform.InverseTransformDirection(nodeTransform.TransformDirection(dockingOrientation));

			Vector3 portDockingOrientation = port.nodeTransform.TransformDirection(port.dockingOrientation);
			Vector3 tv = transform.InverseTransformDirection(portDockingOrientation);

			for(int i = 1; i < snapCount; i++)
			{
				float ff = (360f / snapCount) * i;

				Vector3 tv2 = transform.InverseTransformDirection(Quaternion.AngleAxis(ff, port.nodeTransform.forward) * portDockingOrientation);

				if(Vector3.Angle(tv, tvref) > Vector3.Angle(tv2, tvref))
					tv = tv2;
			}

			Quaternion qt = Quaternion.LookRotation(transform.InverseTransformDirection(nodeTransform.forward), transform.InverseTransformDirection(nodeTransform.TransformDirection(dockingOrientation)));
			Quaternion qc = Quaternion.LookRotation(transform.InverseTransformDirection(-port.nodeTransform.forward), tv);

			rotation = qc * Quaternion.Inverse(qt);


			Vector3 diff = port.nodeTransform.position - nodeTransform.position;

			position = transform.InverseTransformDirection(diff) - ringJoint.anchor;
		}

		void ConfigureRingJoint(ConfigurableJoint joint)
		{
			joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Limited;
			joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Limited;

			joint.xDrive = joint.yDrive = joint.zDrive =
			joint.angularXDrive = joint.angularYZDrive =
				new JointDrive
				{
					positionSpring = 10000f,
					positionDamper = 0f,
					maximumForce = 10000f
				};
		}

		void DisableRingJoint(ConfigurableJoint joint)
		{
			joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Free;
			joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;

			joint.xDrive = joint.yDrive = joint.zDrive =
			joint.angularXDrive = joint.angularYZDrive =
				new JointDrive
				{
					positionSpring = 0f,
					positionDamper = 0f,
					maximumForce = 0f
				};
		}

		ConfigurableJoint BuildRingJoint()
		{
			ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
			joint.connectedBody = ringObject.GetComponent<Rigidbody>();

			joint.autoConfigureConnectedAnchor = false;
			joint.anchor = extendDirection * (maxExtensionLength * 0.5f);
			joint.targetPosition = extendDirection * -(maxExtensionLength * 0.5f);

			ConfigureRingJoint(joint);

			joint.linearLimit = new SoftJointLimit() { limit = maxExtensionLength * 0.5f };

			joint.lowAngularXLimit = new SoftJointLimit() { limit = -40f };
			joint.highAngularXLimit = new SoftJointLimit() { limit = 40f };
				
			joint.angularYLimit = joint.angularZLimit =
				new SoftJointLimit() { limit = 40f };

			joint.breakForce = joint.breakTorque = Mathf.Infinity;

			return joint;
		}

float _captureSlerp;
Vector3 _capturePositionA, _capturePositionB;
Quaternion _captureRotationA, _captureRotationB;

		private void SetCapturedRingPosition(ModuleDockingPortEx port)
		{
			// RingObject
			ringObject.GetComponent<Rigidbody>().isKinematic = true;
			ringObject.GetComponent<Rigidbody>().detectCollisions = false;

			ringObject.transform.parent = port.transform;

_capturePositionA = ringObject.transform.localPosition;
_capturePositionB =
	//		RingObject.transform.position =
				port.ringTransform.position + port.ringTransform.transform.TransformDirection(correctionVector);
_capturePositionB = port.transform.InverseTransformPoint(_capturePositionB); // FEHLER, neu, dann kann ich's per localPosition setzen

			// snap local rotation
			float newY =
				Vector3.SignedAngle(ringObject.transform.TransformDirection(dockingOrientation), port.ringTransform.transform.TransformDirection(dockingOrientation), port.nodeTransform.forward);

			newY /= (360f / snapCount);
			newY = Mathf.Round(newY);
			newY *= (360f / snapCount);

			Quaternion targetLocalRotation = Quaternion.Inverse(otherPort.transform.rotation)
				* Quaternion.AngleAxis(-newY, port.nodeTransform.forward)
					* otherPort.ringTransform.transform.rotation * Quaternion.AngleAxis(180, dockingOrientation);

_captureRotationA = ringObject.transform.localRotation;
_captureRotationB =
//			RingObject.transform.localRotation =
				targetLocalRotation;
		}

		private void BuildJoint()
		{
			// ActiveJoint
			DisableRingJoint(ringJoint);

			// Ring
			SetCapturedRingPosition(otherPort);

			_captureSlerp = 0f;

			// Joint
			joint = gameObject.AddComponent<ConfigurableJoint>();

			joint.connectedBody = otherPort.part.Rigidbody;

			jointBreakForce = Mathf.Min(part.breakingForce, otherPort.part.breakingForce) *
				captureBreakingForceFactor;

			jointBreakTorque = Mathf.Min(part.breakingTorque, otherPort.part.breakingTorque) *
				captureBreakingForceFactor;

			joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Free;
			joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;

			SoftJointLimit angularLimit = default(SoftJointLimit);
			angularLimit.bounciness = 0f;

			SoftJointLimitSpring angularLimitSpring = default(SoftJointLimitSpring);
			angularLimitSpring.spring = 0f;
			angularLimitSpring.damper = 0f;

			joint.highAngularXLimit = angularLimit;
			joint.lowAngularXLimit = angularLimit;
			joint.angularYLimit = angularLimit;
			joint.angularZLimit = angularLimit;
			joint.angularXLimitSpring = angularLimitSpring;
			joint.angularYZLimitSpring = angularLimitSpring;

			SoftJointLimit linearJointLimit = default(SoftJointLimit);
			linearJointLimit.limit = 1f;
			linearJointLimit.bounciness = 0f;

			SoftJointLimitSpring linearJointLimitSpring = default(SoftJointLimitSpring);
			linearJointLimitSpring.damper = 0f;
			linearJointLimitSpring.spring = 0f;

			joint.linearLimit = linearJointLimit;
			joint.linearLimitSpring = linearJointLimitSpring;

			JointDrive angularDrive = new JointDrive { maximumForce = 100f /*capturingForce*/, positionSpring = 100f /*60000f*/, positionDamper = 0.002f };
			joint.angularXDrive = joint.angularYZDrive = angularDrive; 
/*
 * FEHLER, das hier war das alte -> neu ist capturingForce höher (1000f) und positionSpring fix 60000f bzw. PhysicsGlobals.JointForce ...
 * das mit dem positionDamper auf 0.002f wurde übernommen, sollte aber evtl. als parameter geführt werden
 * 
			JointDrive drive =
				new JointDrive
				{
					positionSpring = 100f,
					positionDamper = 0.002f,
					maximumForce = 100f
				};
*/
// FEHLER FEHLER, das mit den 100 ist zwar toll, aber evtl. sollte man's nicht so machen, sondern per config? weiss halt nicht...

			JointDrive linearDrive = new JointDrive { maximumForce = 100f /*capturingForce*/, positionSpring = 100f /*PhysicsGlobals.JointForce*/, positionDamper = 0.002f };
			joint.xDrive = joint.yDrive = joint.zDrive = linearDrive;

			joint.breakForce = float.MaxValue;
			joint.breakTorque = float.MaxValue;

// FEHLER, die Modelle sind oft so ein elender Schrott... unglaublich du... -> geht's so???
joint.anchor = joint.transform.InverseTransformPoint(nodeTransform.position);
iCapturePosition = -100;
		}

		private void CalculateJointTarget()
		{
			Vector3 targetPosition; Quaternion targetRotation;
			DockingHelper.CalculateLatchingPositionAndRotation(this, otherPort, out targetPosition, out targetRotation);

			// invert both values
			jointTargetPosition = -transform.InverseTransformPoint(targetPosition);
			jointTargetRotation = Quaternion.Inverse(Quaternion.Inverse(transform.rotation) * targetRotation);
		}

		private void DestroyJoint()
		{
			// RingObject
			ringObject.transform.parent = null;

			ringObject.GetComponent<Rigidbody>().isKinematic = false;
			ringObject.GetComponent<Rigidbody>().detectCollisions = true;

			// in rare cases, there is still a collider marked as part that can collide with us (when it had been counted as part of the otherPort)
			Collider[] colliders = ringObject.transform.GetComponentsInChildren<Collider>(true);
			CollisionManager.SetCollidersOnVessel(vessel, true, colliders);

			// ActiveJoint
			ConfigureRingJoint(ringJoint);

			ringJoint.targetPosition = ringJoint.transform.InverseTransformPoint(ringObject.transform.position + ringObject.transform.rotation * ringJoint.connectedAnchor) - ringJoint.anchor;
			ringJoint.targetRotation = Quaternion.Inverse(ringJoint.transform.rotation) * ringObject.transform.rotation;

			_pushStep = 1f;

			// Joint
			Destroy(joint);
			joint = null;
		}

		private void UpdatePistons()
		{
			for(int i = 0; i < aLookAt.Count; i++)
			{
				aLookAt[i].part.LookAt(aLookAt[i].target);
				aLookAt[i].part.rotation *= Quaternion.LookRotation(aLookAt[i].direction);

// FEHLER, noch stretch machen, wenn nötig

				if(aLookAt[i].stretch)
					aLookAt[i].part.localScale =
						new Vector3(1f, aLookAt[i].factor * (aLookAt[i].target.position - aLookAt[i].part.position).magnitude, 1f);
			}
		}

		////////////////////////////////////////
		// Update-Functions

		public void FixedUpdate()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				if(vessel && !vessel.packed)
				{
					if((fsm != null) && fsm.Started)
						fsm.FixedUpdateFSM();
				}
			}
		}

		public void Update()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				if(vessel && !vessel.packed)
				{
					if((fsm != null) && fsm.Started)
						fsm.UpdateFSM();

					if(FlightGlobals.fetch.VesselTarget == (ITargetable)this)
					{
						evtSetAsTarget.active = false;
						evtUnsetTarget.active = true;

						if(FlightGlobals.ActiveVessel == vessel)
							FlightGlobals.fetch.SetVesselTarget(null);
						else if((FlightGlobals.ActiveVessel.transform.position - nodeTransform.position).sqrMagnitude > 40000f)
							FlightGlobals.fetch.SetVesselTarget(vessel);
					}
					else
					{
						evtSetAsTarget.active = true;
						evtUnsetTarget.active = false;
					}
				}
			}
		}

		public void LateUpdate()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				if(vessel && !vessel.packed)
				{
					if((fsm != null) && fsm.Started)
						fsm.LateUpdateFSM();
				}

				if(ringObject)
					UpdatePistons();
			}
		}

		////////////////////////////////////////
		// Editor

		private void EditorInitialize()
		{
			InitializeMeshes();
			InitializeLookAt();

			nodeTransform = part.FindModelTransform(nodeTransformName);

			if(nodeTransform && nodeTransform.parent)
				extendDirection = Quaternion.Inverse(nodeTransform.rotation) * nodeTransform.forward;

			GameEvents.onEditorPartEvent.Add(OnEditorPartEvent);

			AttachNode node = null;
			if(referenceAttachNode != string.Empty)
				node = part.FindAttachNode(referenceAttachNode);

			bool isAttached = ((node != null) && node.attachedPart);

			Events["ToggleStartState"].active = !isAttached;
			Events["ToggleStartState"].guiName = (DockStatus == "Inactive") ? "Start 'Inactive'" : "Start 'Ready'";

			Events["ExtendRing"].active = !isAttached;
			Events["RetractRing"].active = false;
		}

		private void EditorResetRing(bool bActivateExtend)
		{
			extendPosition = 0f;

			ringTransform.localPosition = ringOrgLocalPosition;
			ringTransform.localRotation = ringOrgLocalRotation;
			UpdatePistons();

			Events["ExtendRing"].active = bActivateExtend;
			Events["RetractRing"].active = false;
		}

		public override void OnCopy(PartModule fromModule)
		{
			base.OnCopy(fromModule);

			ModuleDockingPortEx fromPort = (ModuleDockingPortEx)fromModule;

			Transform _ringTransform = part.FindModelTransform(ringName);

			_ringTransform.localPosition = fromPort.ringOrgLocalPosition;
			_ringTransform.localRotation = fromPort.ringOrgLocalRotation;
		}

		public void OnEditorPartEvent(ConstructionEventType evt, Part part)
		{
			switch(evt)
			{
			case ConstructionEventType.PartAttached:
				if(referenceAttachNode != string.Empty)
				{
					AttachNode node = this.part.FindAttachNode(referenceAttachNode);
					if((node != null) && node.attachedPart)
					{
						EditorResetRing(false);

						DockStatus = "Attached";
						Events["ToggleStartState"].active = false;
					}
				}
				break;

			case ConstructionEventType.PartDetached:
				if(referenceAttachNode != string.Empty)
				{
					AttachNode node = this.part.FindAttachNode(referenceAttachNode);
					if((node != null) && !node.attachedPart)
					{
						Events["ExtendRing"].active = true;

						DockStatus = "Ready";
						Events["ToggleStartState"].active = true;
					}
				}
				break;
			}
		}

		internal IEnumerator EditorExtendRing()
		{
			while(extendPosition < extensionLength)
			{
				extendPosition = Mathf.Min(extensionLength, extendPosition + extensionSpeed);

				ringTransform.localPosition = ringOrgLocalPosition + extendDirection * extendPosition;
				UpdatePistons();

				yield return new WaitForFixedUpdate();
			}

			Events["ExtendRing"].active = false;
			Events["RetractRing"].active = true;
		}

		internal IEnumerator EditorRetractRing()
		{
			while(extendPosition > 0)
			{
				extendPosition = Mathf.Max(0, extendPosition - extensionSpeed);

				ringTransform.localPosition = ringOrgLocalPosition + extendDirection * extendPosition;
				UpdatePistons();

				yield return new WaitForFixedUpdate();
			}

			Events["ExtendRing"].active = true;
			Events["RetractRing"].active = false;
		}

		////////////////////////////////////////
		// Context Menu

		[KSPField(guiName = "DockingNode status", isPersistant = false, guiActive = true, guiActiveUnfocused = true, unfocusedRange = 20)]
		public string DockStatus = "Ready";

		[KSPField(guiName = "DockingNode distance", isPersistant = false, guiActive = true)]
		public string DockDistance = "-";

		[KSPField(guiName = "DockingNode alignment", isPersistant = false, guiActive = true)]
		public string DockAlignment;

		[KSPField(guiName = "DockingNode angle", isPersistant = false, guiActive = true)]
		public string DockAngle;

		public void Enable()
		{
			fsm.RunEvent(on_enable);
		}

		public void Disable()
		{
			fsm.RunEvent(on_disable);
		}

		[KSPEvent(guiActive = true, guiActiveUnfocused = true, guiName = "Deactivate Port")]
		public void TogglePort()
		{
			if(fsm.CurrentState == st_disabled)
				fsm.RunEvent(on_enable);
			else
				fsm.RunEvent(on_disable);
		}

		[KSPEvent(guiActiveEditor = true)]
		public void ToggleStartState()
		{
			if(DockStatus == "Inactive")
			{
				DockStatus = "Ready";
				Events["ToggleStartState"].guiName = "Start 'Ready'";
			}
			else
			{
				DockStatus = "Inactive";
				Events["ToggleStartState"].guiName = "Start 'Inactive'";
			}
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiActiveUnfocused = false, guiName = "Extend Ring")]
		public void ExtendRing()
		{
			if(HighLogic.LoadedSceneIsFlight)
				fsm.RunEvent(on_extend);
			else if(HighLogic.LoadedSceneIsEditor)
				StartCoroutine(EditorExtendRing());
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiActiveUnfocused = false, guiName = "Retract Ring")]
		public void RetractRing()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				if(otherPort != null)
					otherPort.fsm.RunEvent(otherPort.on_distance_passive);

				fsm.RunEvent(on_retract);
			}
			else if(HighLogic.LoadedSceneIsEditor)
				StartCoroutine(EditorRetractRing());
		}

		[KSPEvent(guiActive = true, guiActiveUnfocused = false, guiName = "Release")]
		public void Release()
		{
			if(otherPort != null)
				otherPort.fsm.RunEvent(otherPort.on_release_passive);

			fsm.RunEvent(on_release);
		}

		[KSPEvent(guiActive = true, guiActiveUnfocused = false, guiName = "Perform Docking")]
		public void PerformDocking()
		{
			fsm.RunEvent(on_preparedocking);
		}

		public void DockToVessel(ModuleDockingPortEx port)
		{
			Debug.Log("Docking to vessel " + port.vessel.GetDisplayName(), gameObject);

			otherPort = port;
			dockedPartUId = otherPort.part.flightID;

			otherPort.otherPort = this;
			otherPort.dockedPartUId = part.flightID;

			DockingHelper.DockVessels(this, otherPort);
		}

		private void DoUndock()
		{
			DockingHelper.UndockVessels(this, otherPort);

			ConfigurableJoint j = part.gameObject.AddComponent<ConfigurableJoint>();
			j.connectedBody = otherPort.part.rb;
			j.axis = j.transform.InverseTransformDirection(nodeTransform.forward);

			j.xMotion = j.yMotion = j.zMotion = ConfigurableJointMotion.Free;
			j.angularXMotion = j.angularYMotion = j.angularZMotion = ConfigurableJointMotion.Free;

			JointDrive strf = new JointDrive();
			strf.maximumForce = 1000000f; strf.positionSpring = 1000000f;

			j.angularXDrive = j.angularYZDrive = strf;
			j.xDrive = j.yDrive = j.zDrive = strf;

			StartCoroutine(killAngVel2(j, 50, Mathf.Min(vessel.GetTotalMass(), otherPort.vessel.GetTotalMass())));

/*
			if(undockEjectionForce > 0.001f)
			{
				part.AddForce(nodeTransform.forward * ((0f - undockEjectionForce) * 0.5f));
				parent.AddForce(nodeTransform.forward * (undockEjectionForce * 0.5f));
			}
*/
		}

static int froverride = 200;

		IEnumerator killAngVel2(ConfigurableJoint j, int fr, float mass)
		{
fr = froverride;

for(int i = 0; i < 4; i++)
			yield return new WaitForFixedUpdate();

JointDrive str = new JointDrive();
			str.maximumForce = 0.007f * mass;
			str.positionSpring = 0.007f * mass;

j.xDrive = str;

			yield return new WaitForFixedUpdate();
			j.targetPosition = Vector3.right; // FEHLER, mal ein Versuch


			do {
			yield return new WaitForFixedUpdate();

// sagen wir mal 0.1 ist die Grenze

				if(
				(j.transform.position // weil unser anchor 0 ist, das hab ich so gebaut
				- j.connectedBody.transform.TransformPoint(j.connectedAnchor)).magnitude > 0.1f)
					break; // Abbruch der Übung

			} while(--fr > 0);

			Destroy(j);
		}

		[KSPEvent(guiActive = true, guiActiveUnfocused = true, externalToEVAOnly = true, unfocusedRange = 2f, guiName = "#autoLOC_6001445")]
		public void Undock()
		{
			DoUndock();

			otherPort.fsm.RunEvent(otherPort.on_undock);
			fsm.RunEvent(on_undock);
		}

		[KSPField(isPersistant = true)]
		public int autoFreeDriftMode = 2;

		private void onChanged_autoFreeDriftMode(object o)
		{
			switch(autoFreeDriftMode)
			{
			case 0:
				Events["ToggleAutoFreeDriftMode"].guiName = "Auto Drift Mode: none"; break;
			case 1:
				Events["ToggleAutoFreeDriftMode"].guiName = "Auto Drift Mode: disable SAS"; break;
			case 2:
				Events["ToggleAutoFreeDriftMode"].guiName = "Auto Drift Mode: disable SAS & RCS"; break;
			}
		}

		public int AutoFreeDriftMode
		{
			get { return autoFreeDriftMode; }
			set { if(autoFreeDriftMode == value) return; autoFreeDriftMode = value; onChanged_autoFreeDriftMode(null); }
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Auto Drift Mode: disable SAS & RCS")]
		public void ToggleAutoFreeDriftMode()
		{
			autoFreeDriftMode = (autoFreeDriftMode + 1) % 3;
			onChanged_autoFreeDriftMode(null);
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "#autoLOC_236028")]
		public void EnableXFeed()
		{
			Events["EnableXFeed"].active = false;
			Events["DisableXFeed"].active = true;
			bool fuelCrossFeed = part.fuelCrossFeed;
			part.fuelCrossFeed = (crossfeed = true);
			if(fuelCrossFeed != crossfeed)
				GameEvents.onPartCrossfeedStateChange.Fire(part);
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "#autoLOC_236030")]
		public void DisableXFeed()
		{
			Events["EnableXFeed"].active = true;
			Events["DisableXFeed"].active = false;
			bool fuelCrossFeed = part.fuelCrossFeed;
			part.fuelCrossFeed = (crossfeed = false);
			if(fuelCrossFeed != crossfeed)
				GameEvents.onPartCrossfeedStateChange.Fire(part);
		}

		////////////////////////////////////////
		// Actions

		[KSPAction("Enable")]
		public void EnableAction(KSPActionParam param)
		{ Enable(); }

		[KSPAction("Disable")]
		public void DisableAction(KSPActionParam param)
		{ Disable(); }

		[KSPAction("Extend")]
		public void ExtendAction(KSPActionParam param)
		{ ExtendRing(); }

		[KSPAction("Retract")]
		public void RetractAction(KSPActionParam param)
		{ RetractRing(); }

		[KSPAction("#autoLOC_6001444", activeEditor = false)]
		public void UndockAction(KSPActionParam param)
		{ Undock(); }

		[KSPAction("#autoLOC_236028")]
		public void EnableXFeedAction(KSPActionParam param)
		{ EnableXFeed(); }

		[KSPAction("#autoLOC_236030")]
		public void DisableXFeedAction(KSPActionParam param)
		{ DisableXFeed(); }

		[KSPAction("#autoLOC_236032")]
		public void ToggleXFeedAction(KSPActionParam param)
		{
			if(crossfeed)
				DisableXFeed();
			else
				EnableXFeed();
		}

		[KSPAction("#autoLOC_6001447")]
		public void MakeReferenceToggle(KSPActionParam act)
		{
			MakeReferenceTransform();
		}

		////////////////////////////////////////
		// Reference / Target

		[KSPEvent(guiActive = true, guiName = "#autoLOC_6001447")]
		public void MakeReferenceTransform()
		{
			part.SetReferenceTransform(controlTransform);
			vessel.SetReferenceTransform(part);
		}

		[KSPEvent(guiActive = false, guiActiveUnfocused = true, externalToEVAOnly = false, unfocusedRange = 200f, guiName = "#autoLOC_6001448")]
		public void SetAsTarget()
		{
			FlightGlobals.fetch.SetVesselTarget(this);
		}

		[KSPEvent(guiActive = false, guiActiveUnfocused = true, externalToEVAOnly = false, unfocusedRange = 200f, guiName = "#autoLOC_6001449")]
		public void UnsetTarget()
		{
			FlightGlobals.fetch.SetVesselTarget(null);
		}

		////////////////////////////////////////
		// IDockable

		private DockInfo dockInfo;

		public Part GetPart()
		{ return part; }

		public Transform GetNodeTransform()
		{ return nodeTransform; }

		public Vector3 GetDockingOrientation()
		{ return dockingOrientation; }

		public int GetSnapCount()
		{ return snapCount; }

		public DockInfo GetDockInfo()
		{ return dockInfo; }

		public void SetDockInfo(DockInfo _dockInfo)
		{
			dockInfo = _dockInfo;

			if(dockInfo == null)
				vesselInfo = null;
			else if(dockInfo.part == (IDockable)this)
				vesselInfo = dockInfo.vesselInfo;
			else
				vesselInfo = dockInfo.targetVesselInfo;
		}

		// returns true, if the port is compatible with the other port
		public bool IsCompatible(IDockable otherPort)
		{
			if(otherPort == null)
				return false;

			ModuleDockingPortEx _otherPort = otherPort.GetPart().GetComponent<ModuleDockingPortEx>();

			if(!_otherPort)
				return false;

			if(!nodeTypesAcceptedS.Contains(_otherPort.nodeType)
			|| !_otherPort.nodeTypesAcceptedS.Contains(nodeType))
				return false;

			return true;
		}

		// returns true, if the port is (passive and) ready to dock with an other (active) port
		public bool IsReadyFor(IDockable otherPort)
		{
			if(otherPort != null)
			{
				if(!IsCompatible(otherPort))
					return false;
			}

			return (fsm.CurrentState == st_ready);
		}

		public ITargetable GetTargetable()
		{
			return (ITargetable)this;
		}

		public bool IsDocked()
		{
			return ((fsm.CurrentState == st_docked) || (fsm.CurrentState == st_preattached));
		}

		public IDockable GetOtherDockable()
		{
			return IsDocked() ? (IDockable)otherPort : null;
		}

		////////////////////////////////////////
		// ITargetable

		public Transform GetTransform()
		{
			return nodeTransform;
		}

		public Vector3 GetObtVelocity()
		{
			return vessel.obt_velocity;
		}

		public Vector3 GetSrfVelocity()
		{
			return vessel.srf_velocity;
		}

		public Vector3 GetFwdVector()
		{
			return nodeTransform.forward;
		}

		public Vessel GetVessel()
		{
			return vessel;
		}

		public string GetName()
		{
			return portName;
		}

		public string GetDisplayName()
		{
			return GetName();
		}

		public Orbit GetOrbit()
		{
			return vessel.orbit;
		}

		public OrbitDriver GetOrbitDriver()
		{
			return vessel.orbitDriver;
		}

		public VesselTargetModes GetTargetingMode()
		{
			return VesselTargetModes.DirectionVelocityAndOrientation;
		}

		public bool GetActiveTargetable()
		{
			return false;
		}

		private DockingPortRenameDialog renameDialog;

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiActiveUnfocused = true, guiName = "Rename Port")]
		public void Rename()
		{
			if(HighLogic.LoadedSceneIsFlight)
				InputLockManager.SetControlLock("dockingPortRenameDialog");

			renameDialog = DockingPortRenameDialog.Spawn(portName, onPortRenameAccept, onPortRenameCancel);
		}

		private void onPortRenameAccept(string newPortName)
		{
			portName = newPortName;
			onPortRenameCancel();
		}

		private void onPortRenameCancel()
		{
			if(HighLogic.LoadedSceneIsFlight)
				InputLockManager.RemoveControlLock("dockingPortRenameDialog");
		}

		////////////////////////////////////////
		// IModuleInfo

		string IModuleInfo.GetModuleTitle()
		{
			return "Docking Port";
		}

		string IModuleInfo.GetInfo()
		{
			string info = "";

			info += "Crossfeed: " + (crossfeed ? "<color=green>supported</color>" : "<color=red>no</color>") + "\n";

			if((electricChargeRequiredLatching > 0f) && (electricChargeRequiredReleasing > 0f))
				info += "\n<b><color=orange>Requires:</color></b>\n- <b>Electric Charge: </b>for latching and releasing";
			else if(electricChargeRequiredLatching > 0f) 
				info += "\n<b><color=orange>Requires:</color></b>\n- <b>Electric Charge: </b>for latching";
			else if(electricChargeRequiredReleasing > 0f)
				info += "\n<b><color=orange>Requires:</color></b>\n- <b>Electric Charge: </b>for releasing";

			return info;
		}

		Callback<Rect> IModuleInfo.GetDrawModulePanelCallback()
		{
			return null;
		}

		string IModuleInfo.GetPrimaryField()
		{
			return null;
		}

		////////////////////////////////////////
		// IResourceConsumer

		private List<PartResourceDefinition> consumedResources;

		public List<PartResourceDefinition> GetConsumedResources()
		{
			return consumedResources;
		}

		////////////////////////////////////////
		// IConstruction

		public bool CanBeDetached()
		{
			return fsm.CurrentState == st_disabled;
		}

		public bool CanBeOffset()
		{
			return fsm.CurrentState == st_disabled;
		}

		public bool CanBeRotated()
		{
			return fsm.CurrentState == st_disabled;
		}

		////////////////////////////////////////
		// Debug

#if DEBUG
/*
		private MultiLineDrawer ld;

		private void DebugInit()
		{
			ld = new MultiLineDrawer();
			ld.Create(null);
		}

		private void DrawPointer(int idx, Vector3 p_vector)
		{
			ld.Draw(idx, idx, Vector3.zero, p_vector);
		}

		private void DrawRelative(int idx, Vector3 p_from, Vector3 p_vector)
		{
			ld.Draw(idx, idx, p_from, p_from + p_vector);
		}

		private void DrawAxis(int idx, Transform p_transform, Vector3 p_vector, bool p_relative, Vector3 p_off)
		{
			ld.Draw(idx, idx, p_transform.position + p_off, p_transform.position + p_off
				+ (p_relative ? p_transform.TransformDirection(p_vector) : p_vector));
		}

		private void DrawAxis(int idx, Transform p_transform, Vector3 p_vector, bool p_relative)
		{ DrawAxis(idx, p_transform, p_vector, p_relative, Vector3.zero); }
*/
#endif

	}
}
