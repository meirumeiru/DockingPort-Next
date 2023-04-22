﻿using System;
using System.Collections;
using System.Collections.Generic;

using KSP.IO;
using UnityEngine;

using DockingPort_Next.Utility;

namespace DockingPort_Next.Module
{
	// FEHLER, Crossfeed noch einrichten... und halt umbauen auf FSM? ... ja, zum Spass... shit ey

	// FEHLER, wir arbeiten bei den Events nie mit "OnCheckCondition" sondern lösen alle manuell aus... kann man sich fragen, ob das gut ist, aber so lange der Event nur von einem Zustand her kommen kann, spielt das wie keine Rolle

	public class ModuleDockingPortEx : PartModule, ITargetable
	{
		// Settings

		[KSPField(isPersistant = false), SerializeField]
		public string nodeTransformName = "dockingNode";

		[KSPField(isPersistant = false), SerializeField]
		public string referenceAttachNode = ""; // if something is connected to this node, then the state is "Attached" (or "Pre-Attached" -> connected in the VAB/SPH)

		[KSPField(isPersistant = false), SerializeField]
		public string controlTransformName = "";

		[KSPField(isPersistant = false), SerializeField]
		public string ringName = "";

		[KSPField(isPersistant = false), SerializeField]
		public Vector3 correctionVector = Vector3.zero; // offset of the "ring center" used in calculations from the real center of the ring-model

		[KSPField(isPersistant = false), SerializeField]
		public Vector3 dockingOrientation = Vector3.zero; // defines the direction of the docking port (when docked at a 0° angle, these local vectors of two ports point into the same direction)

		[KSPField(isPersistant = false), SerializeField]
		public int snapCount = 1;

		[KSPField(isPersistant = false), SerializeField]
		public float extensionLength = 0.18f; // extension (while searching other port)
		
		[KSPField(isPersistant = false), SerializeField]
		public float maxExtensionLength = 0.25f; // maximum extension (used when in active push mode)

		[KSPField(isPersistant = false), SerializeField]
		public float extensionSpeed = 0.005f;

		[KSPField(isPersistant = false), SerializeField]
		public float pushSpeed = 0.01f;

// FEHLER, nodeTypes und so Zeugs noch einbauen -> damit ich nur docken kann mit den passenden Teils und nicht mit allem

		[KSPField(isPersistant = true)]
		public bool crossfeed = true;

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
		public Transform controlTransform;

		public KerbalFSM fsm;

// FEHLER, pre-attached noch einbauen

		public KFSMState st_ready;			// "passive"

		public KFSMState st_extending;		// "activating"
		public KFSMState st_retracting;		// "deactivating"
		public KFSMState st_extended;		// "active" / "searching"
		
		public KFSMState st_approaching;	// port found
		public KFSMState st_approached;		// same as approaching, but for passive side

		public KFSMState st_push;			// try to push the ring into the other port
		public KFSMState st_restore;		// pull the ring back from the other port (opposite of st_push)
		
		public KFSMState st_captured;		// the rings have a first connection
		public KFSMState st_target;			// same as captured, but for the passive side
		public KFSMState st_uncaptured;		// after a capture or latch, the rings have been detached again -> maybe for an abort of the docking
		
		public KFSMState st_latched;		// the rings have a stable connection and the system is ready for orienting, pullback and docking
		
		public KFSMState st_preparedocking;	// orienting and retracting in progress
		public KFSMState st_predocked;		// ready to dock (the real docking process that makes 1 ship out of the 2)
		
		public KFSMState st_docked;
		public KFSMState st_preattached;

		public KFSMState st_disabled;


		public KFSMEvent on_enable;
		public KFSMEvent on_disable;

		public KFSMEvent on_extend;
		public KFSMEvent on_retract;

		public KFSMEvent on_extended;
		public KFSMEvent on_retracted;

		public KFSMEvent on_approaching;
		public KFSMEvent on_distance;

		public KFSMEvent on_approached;
		public KFSMEvent on_distanced;

		public KFSMEvent on_push;
		public KFSMEvent on_restore;

		public KFSMEvent on_capture;
		public KFSMEvent on_release;

		public KFSMEvent on_captured;
		public KFSMEvent on_released;

		public KFSMEvent on_latch;

		public KFSMEvent on_preparedocking;
		public KFSMEvent on_predock;

		public KFSMEvent on_dock;

		public KFSMEvent on_undock;

		// Ring

		private Transform Ring = null;
		private Transform originalRingParent;
		private Vector3 originalRingLocalPosition;
		private Quaternion originalRingLocalRotation;

		private GameObject RingObject;
		private ConfigurableJoint ActiveJoint;

		private Vector3 extendDirection;
		private float extendPosition = 0f;

		private float detectionDistance = 5f;

		private ModuleDockingPortEx otherPort;

// FEHLER, nicht nötig als Variable hier... denke ich mal... weil das nur immer temporär existiert
		private Quaternion ActiveJointTargetRotation;
		private Vector3 ActiveJointTargetPosition;

		private float _pushStep = 0f;

		private float captureDistance = 0.005f;

		private ConfigurableJoint CaptureJoint;

		private Vector3 originalRingObjectLocalPosition;
		private Quaternion originalRingObjectLocalRotation;

		private Quaternion CaptureJointTargetRotation;
		private Vector3 CaptureJointTargetPosition;

		private float lastPreLatchDistance;

		private int iCapturePosition;
		private int iPos = 0;

		private float _rotStep = 0f;
		private float _transStep = 0f;	// FEHLER, ablösen den Müll hier

		// Docking

		private uint dockedPartUId;

		private DockingPortStatus _state = null;

		// Packed / OnRails

		private Vector3 ringRelativePosition;
		private Quaternion ringRelativeRotation;

		private bool followOtherPort = false;

		private Vector3 otherPortRelativePosition;
		private Quaternion otherPortRelativeRotation;

		////////////////////////////////////////
		// Constructor

		public ModuleDockingPortEx()
		{
		}

		////////////////////////////////////////
		// Callbacks

		public override void OnAwake()
		{
			DebugInit();

			part.dockingPorts.AddUnique(this);
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			LoadLookAt(node);

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

			part.fuelCrossFeed = crossfeed;
		}

		public DockingPortStatus BuildState()
		{
			DockingPortStatus state = new DockingPortStatus();

			if((DockStatus == "Extending ring")
			|| (DockStatus == "Retracting ring")
			|| (DockStatus == "Searching")
			|| (DockStatus == "Approaching")
			|| (DockStatus == "Push ring")
			|| (DockStatus == "Restore ring")
			|| (DockStatus == "Capture released"))
			{
				state.ringPosition = part.transform.InverseTransformPoint(RingObject.transform.position);
				state.ringRotation = Quaternion.Inverse(part.transform.rotation) * RingObject.transform.rotation;

				state.extendPosition = extendPosition;

				state.activeJointTargetPosition = ActiveJoint.targetPosition;
				state.activeJointTargetRotation = ActiveJoint.targetRotation;

				state._pushStep = _pushStep;
			}

			if((DockStatus == "Captured")
			|| (DockStatus == "Latched")
			|| (DockStatus == "Retracting ring"))
			{
				state.ringPosition = RingObject.transform.localPosition;
				state.ringRotation = RingObject.transform.localRotation;

				state.extendPosition = extendPosition;

				state.activeJointTargetPosition = ActiveJoint.targetPosition;
				state.activeJointTargetRotation = ActiveJoint.targetRotation;

				state.originalRingObjectLocalPosition = originalRingObjectLocalPosition;
				state.originalRingObjectLocalRotation = originalRingObjectLocalRotation;

				state.otherPortRelativePosition = Quaternion.Inverse(otherPort.part.transform.rotation) * (vessel.transform.position - otherPort.part.transform.position);
				state.otherPortRelativeRotation = Quaternion.Inverse(otherPort.part.transform.rotation) * vessel.transform.rotation;
			}

// FEHLER, bei dem bin ich nicht ganz sicher -> beim Retracting auch nicht

			if(DockStatus == "Docking")
			{
				state.ringPosition = RingObject.transform.localPosition;
				state.ringRotation = RingObject.transform.localRotation;

				state.extendPosition = extendPosition;

				state.originalRingObjectLocalPosition = originalRingObjectLocalPosition;
				state.originalRingObjectLocalRotation = originalRingObjectLocalRotation;

				state.otherPortRelativePosition = Quaternion.Inverse(otherPort.part.transform.rotation) * (vessel.transform.position - otherPort.part.transform.position);
				state.otherPortRelativeRotation = Quaternion.Inverse(otherPort.part.transform.rotation) * vessel.transform.rotation;
			}

			if(DockStatus == "Docked")
			{
// FEHLER, das hier noch machen... das wird oft vorkommen und sollte schon stimmen dann... also echt jetzt
			}

// FEHLER FEHLER hier noch weiter machen dann... -> aktuell tun wir's immer
	//			state.captureJointTargetPosition = CaptureJoint.targetPosition;
	//			state.captureJointTargetRotation = CaptureJointTargetRotation;
					// FEHLER, das ist doch sinnlos hier

	//		state._rotStep = _rotStep;
	//		state._transStep = _transStep;

			return state;
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

			node.AddValue("state", (string)(((fsm != null) && (fsm.Started)) ? fsm.currentStateName : "Ready"));

			node.AddValue("dockUId", dockedPartUId);

			if(vesselInfo != null)
				vesselInfo.Save(node.AddNode("DOCKEDVESSEL"));

			BuildState().Save(node.AddNode("PORTSTATUS"));
		}

		public override void OnStart(StartState st)
		{
			base.OnStart(st);

			evtSetAsTarget = base.Events["SetAsTarget"];
			evtUnsetTarget = base.Events["UnsetTarget"];

			nodeTransform = base.part.FindModelTransform(nodeTransformName);
			if(!nodeTransform)
			{
				Debug.LogWarning("[Docking Node Module]: WARNING - No node transform found with name " + nodeTransformName, base.part.gameObject);
				return;
			}
			if(controlTransformName == string.Empty)
				controlTransform = base.part.transform;
			else
			{
				controlTransform = base.part.FindModelTransform(controlTransformName);
				if(!controlTransform)
				{
					Debug.LogWarning("[Docking Node Module]: WARNING - No control transform found with name " + controlTransformName, base.part.gameObject);
					controlTransform = base.part.transform;
				}
			}

			StartCoroutine(WaitAndInitialize(st));

			StartCoroutine(WaitAndInitializeDockingNodeFix());
		}

		// FEHLER, ist 'n Quickfix, solange der blöde Port noch drüber hängt im Part...
		public IEnumerator WaitAndInitializeDockingNodeFix()
		{
			ModuleDockingNode DockingNode = part.FindModuleImplementing<ModuleDockingNode>();

			if(DockingNode)
			{
				while((DockingNode.fsm == null) || (!DockingNode.fsm.Started))
					yield return null;

				DockingNode.fsm.RunEvent(DockingNode.on_disable);
			}
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

			Events["EnableXFeed"].active = !crossfeed;
			Events["DisableXFeed"].active = crossfeed;

			if(dockedPartUId != 0)
			{
				Part otherPart;

				while(!(otherPart = FlightGlobals.FindPartByID(dockedPartUId)))
					yield return null;

				otherPort = otherPart.GetComponent<ModuleDockingPortEx>();

				while(otherPort.Ring == null)
					yield return null;

		// FEHLER, logo, das könnte auch er laden... aber... na ja...
				otherPort.otherPort = this;
				otherPort.dockedPartUId = part.flightID;
			}

			if((DockStatus == "Extending ring")
			|| (DockStatus == "Retracting ring")
			|| (DockStatus == "Searching")
			|| (DockStatus == "Approaching")
			|| (DockStatus == "Push ring")
			|| (DockStatus == "Restore ring"))
			{
				BuildRingObject();
				ActiveJoint = BuildActiveJoint();

				RingObject.transform.position = part.transform.TransformPoint(_state.ringPosition);
				RingObject.transform.rotation = part.transform.rotation * _state.ringRotation;

				extendPosition = _state.extendPosition;

				ActiveJoint.targetPosition = _state.activeJointTargetPosition;
				ActiveJoint.targetRotation = _state.activeJointTargetRotation;

				_pushStep = _state._pushStep;

				// Pack

				RingObject.GetComponent<Rigidbody>().isKinematic = true;
				RingObject.GetComponent<Rigidbody>().detectCollisions = false;

				RingObject.transform.parent = transform;
			}

			if(DockStatus == "Captured")
			{
				BuildRingObject();
				ActiveJoint = BuildActiveJoint();

				RingObject.transform.position = otherPort.transform.TransformPoint(_state.originalRingObjectLocalPosition);
				RingObject.transform.rotation = otherPort.transform.rotation * _state.originalRingObjectLocalRotation;

				extendPosition = _state.extendPosition;

				ActiveJoint.targetPosition = _state.activeJointTargetPosition;
				ActiveJoint.targetRotation = _state.activeJointTargetRotation;

				_pushStep = _state._pushStep;

		// FEHLER, hier machen wir wieder einen super schwachen Joint und fangen neu an mit dem Latching... das ist so gewollt (im Moment zumindest)
				BuildCaptureJoint(otherPort);
				BuildCaptureJoint2();

				// Pack

				ringRelativePosition = RingObject.transform.localPosition;
				ringRelativeRotation = RingObject.transform.localRotation;

				RingObject.transform.parent = transform;

				otherPortRelativePosition = _state.otherPortRelativePosition;
				otherPortRelativeRotation = _state.otherPortRelativeRotation;

				followOtherPort = true;
			}

			if((DockStatus == "Latched")
			|| (DockStatus == "Retracting ring"))
			{
				BuildRingObject();
				ActiveJoint = BuildActiveJoint();

				RingObject.transform.position = otherPort.transform.TransformPoint(_state.originalRingObjectLocalPosition);
				RingObject.transform.rotation = otherPort.transform.rotation * _state.originalRingObjectLocalRotation;

				extendPosition = _state.extendPosition;

				ActiveJoint.targetPosition = _state.activeJointTargetPosition;
				ActiveJoint.targetRotation = _state.activeJointTargetRotation;

				_pushStep = _state._pushStep;

		// FEHLER, hier machen wir wieder einen super schwachen Joint und fangen neu an mit dem Latching... das ist so gewollt (im Moment zumindest)

				BuildCaptureJoint(otherPort);
				BuildCaptureJoint2();

				RingObject.transform.localPosition =
						_capturePositionB;

				RingObject.transform.localRotation =
						_captureRotationB;

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

				CaptureJoint.xDrive = drive;
				CaptureJoint.yDrive = drive;
				CaptureJoint.zDrive = drive;

				CaptureJoint.slerpDrive = drive;

				// Pack

				ringRelativePosition = RingObject.transform.localPosition;
				ringRelativeRotation = RingObject.transform.localRotation;

				RingObject.transform.parent = transform;

				otherPortRelativePosition = _state.otherPortRelativePosition;
				otherPortRelativeRotation = _state.otherPortRelativeRotation;

				followOtherPort = true;
			}

// FEHLER, fehlt noch total
			if(DockStatus == "Docking")
			{
			}

			if(DockStatus == "Docked")
			{
			}

			if(DockStatus == "Ready")
			{
				// fix state if attached to other port

				if(referenceAttachNode != string.Empty)
				{
					AttachNode node = part.FindAttachNode(referenceAttachNode);
					if((node != null) && node.attachedPart)
					{
						ModuleDockingPortEx DockingNodeEx_ = node.attachedPart.GetComponent<ModuleDockingPortEx>();

						if(DockingNodeEx_)
						{
							otherPort = DockingNodeEx_;
							dockedPartUId = otherPort.part.flightID;

							DockStatus = "Attached";
						}
					}
				}
			}

			SetupFSM();

			fsm.StartFSM(DockStatus);
		}

		public void Start()
		{
			GameEvents.onVesselGoOnRails.Add(OnPack);
			GameEvents.onVesselGoOffRails.Add(OnUnpack);

		//	GameEvents.onFloatingOriginShift.Add(OnFloatingOriginShift);
		}

		public void OnDestroy()
		{
			if(RingObject != null)
				Destroy(RingObject);

			GameEvents.onVesselGoOnRails.Remove(OnPack);
			GameEvents.onVesselGoOffRails.Remove(OnUnpack);

		//	GameEvents.onFloatingOriginShift.Remove(OnFloatingOriginShift);
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
					RingObject.GetComponent<Rigidbody>().isKinematic = true;
					RingObject.GetComponent<Rigidbody>().detectCollisions = false;

					RingObject.transform.parent = transform;
				}

				if((DockStatus == "Captured")
				|| (DockStatus == "Latched"))
				{
					ringRelativePosition = RingObject.transform.localPosition;
					ringRelativeRotation = RingObject.transform.localRotation;

					RingObject.transform.parent = transform;

					otherPortRelativePosition = Quaternion.Inverse(otherPort.part.transform.rotation) * (vessel.transform.position - otherPort.part.transform.position);
					otherPortRelativeRotation = Quaternion.Inverse(otherPort.part.transform.rotation) * vessel.transform.rotation;

					followOtherPort = true;
				}
			}
		}

		private void OnUnpack(Vessel v)
		{
			if(vessel == v)
			{
				if((DockStatus == "Captured")
				|| (DockStatus == "Latched"))
				{
					followOtherPort = false;
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
				RingObject.GetComponent<Rigidbody>().isKinematic = false;
				RingObject.GetComponent<Rigidbody>().detectCollisions = true;

				RingObject.transform.parent = null;
			}

			if((DockStatus == "Captured")
			|| (DockStatus == "Latched"))
			{
				RingObject.transform.parent = otherPort.transform;

				RingObject.transform.localPosition = ringRelativePosition;
				RingObject.transform.localRotation = ringRelativeRotation;
			}
		}
	/*
		private void OnFloatingOriginShift(Vector3d offset, Vector3d nonFrame)
		{
			if(RingObject)
				RingObject.transform.position += offset;
		}
	*/
		////////////////////////////////////////
		// Functions

		private void LoadLookAt(ConfigNode node)
		{
			if(aLookAtInfo == null)
			{
				if((part.partInfo != null) && (part.partInfo.partPrefab != null))
				{
					ModuleDockingPortEx prefabModule = (ModuleDockingPortEx)part.partInfo.partPrefab.Modules["ModuleDockingPortEx"];
					if(prefabModule != null)
					{
						aLookAtInfo = prefabModule.aLookAtInfo;
					}
				}
				else // I assume, that I'm the prefab then
				{
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
			aLookAt = new List<LookAt>(aLookAtInfo.Count);

			for(int i = 0; i < aLookAtInfo.Count; i++)
			{
				LookAtInfo info = aLookAtInfo[i];

				LookAt l = new LookAt();

				l.part = KSPUtil.FindInPartModel(part.transform, info.partName);
				l.target = KSPUtil.FindInPartModel(part.transform, info.targetName);
				l.direction = info.direction;
				l.stretch = info.stretch;
				if(l.stretch)
					l.factor = l.part.localScale.y / (l.target.position - l.part.position).magnitude;

				aLookAt.Add(l);
			}

			UpdatePistons();
		}

		private void InitializeMeshes()
		{
			if(Ring != null)
				return;

			Ring = KSPUtil.FindInPartModel(transform, ringName);

			originalRingParent = Ring.parent;
			originalRingLocalPosition = Ring.localPosition;
			originalRingLocalRotation = Ring.localRotation;
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

				Events["ExtendRing"].active = true;
			};
			st_ready.OnFixedUpdate = delegate
			{
			};
			st_ready.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_ready);

			st_extending = new KFSMState("Extending ring");
			st_extending.OnEnter = delegate(KFSMState from)
			{
				if(from != st_ready)
					return;

				Events["TogglePort"].active = false;

				if(RingObject == null)
					BuildRingObject();

				if(ActiveJoint == null)
					ActiveJoint = BuildActiveJoint();

				Events["ExtendRing"].active = false;
				Events["RetractRing"].active = true;
			};
			st_extending.OnFixedUpdate = delegate
			{
				if(extendPosition < extensionLength)
				{
					extendPosition = Mathf.Min(extensionLength, extendPosition + extensionSpeed);

					ActiveJoint.targetPosition = extendDirection * (extendPosition - (maxExtensionLength * 0.5f));
				}
				else
					fsm.RunEvent(on_extended);
			};
			st_extending.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_extending);

			st_retracting = new KFSMState("Retracting ring");
			st_retracting.OnEnter = delegate(KFSMState from)
			{
				otherPort = null;
				dockedPartUId = 0;

				Events["RetractRing"].active = false;
				Events["ExtendRing"].active = true;
			};
			st_retracting.OnFixedUpdate = delegate
			{
				if(extendPosition > 0f)
				{
					extendPosition = Mathf.Max(0f, extendPosition - extensionSpeed);

					ActiveJoint.targetPosition = extendDirection * (extendPosition - (maxExtensionLength * 0.5f));
				}
				else
					fsm.RunEvent(on_retracted);
			};
			st_retracting.OnLeave = delegate(KFSMState to)
			{
				if(to != st_ready)
					return;

				Destroy(ActiveJoint);
				ActiveJoint = null;

				DestroyRingObject();
				RingObject = null;

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
			};
			st_extended.OnFixedUpdate = delegate
			{
				Vector3 distance; float angle;

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

						ModuleDockingPortEx DockingNodeEx_ = partModule.GetComponent<ModuleDockingPortEx>();

						if(DockingNodeEx_ == null)
							continue;

						if(DockingNodeEx_.fsm.CurrentState != DockingNodeEx_.st_ready)
							continue;

						distance = DockingNodeEx_.Ring.transform.position - RingObject.transform.position;

						if(distance.magnitude < detectionDistance)
						{
							DockDistance = distance.magnitude.ToString();

							angle = Vector3.Angle(nodeTransform.forward, -DockingNodeEx_.nodeTransform.forward);

							if((angle <= 15f) && (distance.magnitude <= 1f))
							{
								otherPort = DockingNodeEx_;
								dockedPartUId = otherPort.part.flightID;

								fsm.RunEvent(on_approaching);
								return;
							}
						}
					}
				}
			};
			st_extended.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_extended);

			st_approaching = new KFSMState("Approaching");
			st_approaching.OnEnter = delegate(KFSMState from)
			{
				Events["RetractRing"].active = true;

				otherPort.otherPort = this;
				otherPort.dockedPartUId = part.flightID;

				otherPort.fsm.RunEvent(otherPort.on_approached);

				_pushStep = 0f;
			};
			st_approaching.OnFixedUpdate = delegate
			{
				float relevantDistance = (otherPort.Ring.transform.position - RingObject.transform.position).magnitude - correctionVector.magnitude;

				if(relevantDistance < (maxExtensionLength - extensionLength))
					fsm.RunEvent(on_push);
				else
				{
					Vector3 distance = otherPort.Ring.transform.position - RingObject.transform.position;

					if(distance.magnitude < detectionDistance)
					{
						float angle = Vector3.Angle(nodeTransform.forward, -otherPort.nodeTransform.forward);

						if(angle <= 15f)
						{
							DockDistance = distance.magnitude.ToString();
							return;
						}
					}

					fsm.RunEvent(on_distance);
				}
			};
			st_approaching.OnLeave = delegate(KFSMState to)
			{
				if((to == st_extended) || (to == st_retracting))
				{
					otherPort.fsm.RunEvent(otherPort.on_distanced);
				}
			};
			fsm.AddState(st_approaching);

			st_approached = new KFSMState("Approached");
			st_approached.OnEnter = delegate(KFSMState from)
			{
				Events["TogglePort"].active = false;
				Events["ExtendRing"].active = false;
			};
			st_approached.OnFixedUpdate = delegate
			{
			};
			st_approached.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_approached);

			st_push = new KFSMState("Push ring");
			st_push.OnEnter = delegate(KFSMState from)
			{
				Events["RetractRing"].active = true;
			};
			st_push.OnFixedUpdate = delegate
			{
				float relevantDistance = (otherPort.Ring.transform.position - RingObject.transform.position).magnitude - correctionVector.magnitude;

				DockDistance = (otherPort.Ring.transform.position - RingObject.transform.position).magnitude.ToString();

				if(relevantDistance <= captureDistance)
					fsm.RunEvent(on_capture);
				else if(relevantDistance > (maxExtensionLength - extensionLength) * 1.4f)
					fsm.RunEvent(on_restore);
				else
				{
					CalculateActiveJointRotationAndPosition(otherPort, out ActiveJointTargetRotation, out ActiveJointTargetPosition);

					_pushStep = Mathf.Min(1.0f, _pushStep + pushSpeed);

					ActiveJoint.targetRotation = Quaternion.Slerp(Quaternion.identity, ActiveJointTargetRotation, _pushStep);
					ActiveJoint.targetPosition = Vector3.Slerp(extendDirection * (extendPosition - (maxExtensionLength * 0.5f)), ActiveJointTargetPosition, _pushStep);
				}
			};
			st_push.OnLeave = delegate(KFSMState to)
			{
				if((to == st_restore) || (to == st_retracting))
				{
					otherPort.fsm.RunEvent(otherPort.on_distanced);
				}
			};
			fsm.AddState(st_push);

			st_restore = new KFSMState("Restore ring");
			st_restore.OnEnter = delegate(KFSMState from)
			{
				Events["RetractRing"].active = true;
			};
			st_restore.OnFixedUpdate = delegate
			{
				float relevantDistance;

				if(otherPort)
				{
					relevantDistance = (otherPort.Ring.transform.position - RingObject.transform.position).magnitude - correctionVector.magnitude;

					DockDistance = (otherPort.Ring.transform.position - RingObject.transform.position).magnitude.ToString();
				}
				else
					relevantDistance = 10f;

				if(relevantDistance < (maxExtensionLength - extensionLength))
					fsm.RunEvent(on_push);
				else
				{
					if(otherPort)
						CalculateActiveJointRotationAndPosition(otherPort, out ActiveJointTargetRotation, out ActiveJointTargetPosition);

					_pushStep = Mathf.Max(0f, _pushStep - pushSpeed);

					if(_pushStep > 0f)
					{
						ActiveJoint.targetRotation = Quaternion.Slerp(Quaternion.identity, ActiveJointTargetRotation, _pushStep);
						ActiveJoint.targetPosition = Vector3.Slerp(extendDirection * (extendPosition - (maxExtensionLength * 0.5f)), ActiveJointTargetPosition, _pushStep);
					}
					else
					{
						_pushStep = 0f;

						ActiveJoint.targetRotation = Quaternion.identity;
						ActiveJoint.targetPosition = extendDirection * (extendPosition - (maxExtensionLength * 0.5f));

						otherPort = null;
						dockedPartUId = 0;

						fsm.RunEvent(on_extended);
					}
				}
			};
			st_restore.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_restore);
		
			st_captured = new KFSMState("Captured");
			st_captured.OnEnter = delegate(KFSMState from)
			{
				BuildCaptureJoint(otherPort);
				BuildCaptureJoint2();

				Events["RetractRing"].active = false;
				Events["Release"].active = true;

				otherPort.fsm.RunEvent(otherPort.on_captured);
			};
			st_captured.OnFixedUpdate = delegate
			{
				if(_captureSlerp < 1f)
				{
					_captureSlerp = Math.Min(1f, _captureSlerp + 0.05f);

					RingObject.transform.localPosition =
						Vector3.Slerp(_capturePositionA, _capturePositionB, _captureSlerp);

					RingObject.transform.localRotation =
						Quaternion.Slerp(_captureRotationA, _captureRotationB, _captureSlerp);

					return;
				}

				if(--iPos > 0)
					return;

				++iCapturePosition;
				iPos = 8;

				{
					float f, d;

					if(iCapturePosition < 0)
					{
						f = Mathf.Max((iCapturePosition + 50) * 50f, 100f);
						d = 0.002f;

						float preLatchDistance = (otherPort.nodeTransform.position - nodeTransform.position).magnitude;

						if(Mathf.Abs(preLatchDistance - lastPreLatchDistance) < 0.001f)
							iPos = 1;
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

					CaptureJoint.xDrive = drive;
					CaptureJoint.yDrive = drive;
					CaptureJoint.zDrive = drive;

					CaptureJoint.slerpDrive = drive;

					if(iCapturePosition >= 25)
					{
						fsm.RunEvent(on_latch);
					}
				}
			};
			st_captured.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_captured);

			st_target = new KFSMState("Target");
			st_target.OnEnter = delegate(KFSMState from)
			{
				Events["TogglePort"].active = false;
				Events["ExtendRing"].active = false;
			};
			st_target.OnFixedUpdate = delegate
			{
			};
			st_target.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_target);

			st_uncaptured = new KFSMState("Capture released");
			st_uncaptured.OnEnter = delegate(KFSMState from)
			{
				DestroyCaptureJoint();

				Events["Release"].active = false;
				Events["PerformDocking"].active = false;
				Events["RetractRing"].active = true;

				otherPort.fsm.RunEvent(otherPort.on_released);
			};
			st_uncaptured.OnFixedUpdate = delegate
			{
				float relevantDistance = (otherPort.Ring.transform.position - RingObject.transform.position).magnitude - correctionVector.magnitude;

				DockDistance = (otherPort.Ring.transform.position - RingObject.transform.position).magnitude.ToString();

				if(relevantDistance > (maxExtensionLength - extensionLength) * 1.4f)
				{
					otherPort = null;
					dockedPartUId = 0;

					fsm.RunEvent(on_restore);
				}
			};
			st_uncaptured.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_uncaptured);
		
			st_latched = new KFSMState("Latched");
			st_latched.OnEnter = delegate(KFSMState from)
			{
				Events["Release"].active = true;
				Events["PerformDocking"].active = true;
			};
			st_latched.OnFixedUpdate = delegate
			{
			};
			st_latched.OnLeave = delegate(KFSMState to)
			{
				Events["PerformDocking"].active = false;
			};
			fsm.AddState(st_latched);
		
			st_preparedocking = new KFSMState("Retracting ring");
			st_preparedocking.OnEnter = delegate(KFSMState from)
			{
				Destroy(ActiveJoint);
				ActiveJoint = null;

				Events["Release"].active = false;
				Events["PerformDocking"].active = false;
				// OPTION: abort docking?
			};
			st_preparedocking.OnFixedUpdate = delegate
			{
				if((_rotStep > 0.01f) || (_transStep > 0.01f))
				{
			//		DrawRelative(2, otherPort.DockingNode.nodeTransform.position, otherPort.DockingNode.nodeTransform.forward);
			//		DrawRelative(4, otherPort.DockingNode.nodeTransform.position, otherPort.DockingNode.nodeTransform.up);

			//		DrawRelative(6, DockingNode.nodeTransform.position, DockingNode.nodeTransform.forward);
			//		DrawRelative(8, DockingNode.nodeTransform.position, DockingNode.nodeTransform.up);

					DockStatus = "docking (orientation)";
// FEHLER, das wird überschrieben... mal sehen ob wir noch was tun hier oder ob wir's halt lassen die Info

					_rotStep -= 1f / (Quaternion.Angle(Quaternion.identity, CaptureJointTargetRotation) / 0.008f);
					if(_rotStep < 0) _rotStep = 0f;

					CaptureJoint.targetRotation = Quaternion.Slerp(CaptureJointTargetRotation, Quaternion.identity, _rotStep);

					// Abstand von meiner Achse bestimmen
					Vector3 diff = otherPort.nodeTransform.position - nodeTransform.position;
					Vector3 diffp = Vector3.ProjectOnPlane(diff, nodeTransform.forward);

					Vector3 diffpl = /*CaptureJoint.transform.rotation **/ (Quaternion.Inverse(nodeTransform.rotation) * diffp);
					diffpl = Quaternion.Inverse(CaptureJoint.transform.rotation) * diffp;

					if(diffpl.magnitude < 0.0005f)
					{
						CaptureJoint.targetPosition -= diffpl;
						_transStep = 0f;
					}
					else
						CaptureJoint.targetPosition -= diffpl.normalized * 0.0005f;
				}
				else
				{
					DockStatus = "docking (retracting)";
// FEHLER, das wird überschrieben... mal sehen ob wir noch was tun hier oder ob wir's halt lassen die Info

					CaptureJoint.targetRotation = CaptureJointTargetRotation;

					Vector3 diff = otherPort.nodeTransform.position - nodeTransform.position;
					diff = CaptureJoint.transform.InverseTransformDirection(diff);

					if(diff.magnitude < 0.0005f)
					{
						CaptureJoint.targetPosition -= diff;

						fsm.RunEvent(on_predock);
					}
					else
						CaptureJoint.targetPosition -= diff.normalized * 0.0005f;
				}
			};
			st_preparedocking.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_preparedocking);

			st_predocked = new KFSMState("Docking");
			st_predocked.OnEnter = delegate(KFSMState from)
			{
				iPos = 10;
			};
			st_predocked.OnFixedUpdate = delegate
			{
				if(--iPos < 0)
					fsm.RunEvent(on_dock);
			};
			st_predocked.OnLeave = delegate(KFSMState to)
			{
				DestroyRingObject();
				RingObject = null;

				UpdatePistons();

				extendPosition = 0f;

				DockToVessel(otherPort);

				Destroy(CaptureJoint);
				CaptureJoint = null;

				otherPort.fsm.RunEvent(otherPort.on_dock);
			};
			fsm.AddState(st_predocked);
		
			st_docked = new KFSMState("Docked");
			st_docked.OnEnter = delegate(KFSMState from)
			{
				Events["Undock"].active = true;
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

				Events["ExtendRing"].active = false;
			};
			st_disabled.OnFixedUpdate = delegate
			{
			};
			st_disabled.OnLeave = delegate(KFSMState to)
			{
			};
			fsm.AddState(st_disabled);


			on_enable = new KFSMEvent("Enable");
			on_enable.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_enable.GoToStateOnEvent = st_ready;
			fsm.AddEvent(on_enable, st_disabled);

			on_disable = new KFSMEvent("Disable");
			on_disable.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_disable.GoToStateOnEvent = st_disabled;
			fsm.AddEvent(on_disable, st_ready);

			on_extend = new KFSMEvent("Extend Ring");
			on_extend.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_extend.GoToStateOnEvent = st_extending;
			fsm.AddEvent(on_extend, st_ready);

			on_retract = new KFSMEvent("Retract Ring");
			on_retract.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_retract.GoToStateOnEvent = st_retracting;
			fsm.AddEvent(on_retract, st_extending, st_extended, st_approaching, st_push);

			on_extended = new KFSMEvent("Ring extended");
			on_extended.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_extended.GoToStateOnEvent = st_extended;
			fsm.AddEvent(on_extended, st_extending, st_restore);

			on_retracted = new KFSMEvent("Ring retracted");
			on_retracted.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_retracted.GoToStateOnEvent = st_ready;
			fsm.AddEvent(on_retracted, st_retracting);


			on_approaching = new KFSMEvent("Approaching");
			on_approaching.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_approaching.GoToStateOnEvent = st_approaching;
			fsm.AddEvent(on_approaching, st_extended);

			on_distance = new KFSMEvent("Distancing");
			on_distance.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_distance.GoToStateOnEvent = st_extended;
			fsm.AddEvent(on_distance, st_approaching);

			on_approached = new KFSMEvent("Approached");
			on_approached.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_approached.GoToStateOnEvent = st_approached;
			fsm.AddEvent(on_approached, st_ready);

			on_distanced = new KFSMEvent("Distanced");
			on_distanced.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_distanced.GoToStateOnEvent = st_ready;
			fsm.AddEvent(on_distanced, st_approached);

			on_push = new KFSMEvent("Push Ring");
			on_push.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_push.GoToStateOnEvent = st_push;
			fsm.AddEvent(on_push, st_approaching);

			on_restore = new KFSMEvent("Restore Ring");
			on_restore.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_restore.GoToStateOnEvent = st_restore;
			fsm.AddEvent(on_restore, st_push, st_uncaptured);

			on_capture = new KFSMEvent("Capture");
			on_capture.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_capture.GoToStateOnEvent = st_captured;
			fsm.AddEvent(on_capture, st_push);

			on_release = new KFSMEvent("Release capture");
			on_release.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_release.GoToStateOnEvent = st_uncaptured;
			fsm.AddEvent(on_release, st_captured, st_latched);

			on_captured = new KFSMEvent("Capture (as target)");
			on_captured.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_captured.GoToStateOnEvent = st_target;
			fsm.AddEvent(on_captured, st_approached, st_ready);

			on_released = new KFSMEvent("Release capture (as target)");
			on_released.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_released.GoToStateOnEvent = st_ready;
			fsm.AddEvent(on_released, st_target);

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
			fsm.AddEvent(on_dock, st_predocked, st_target);

			on_undock = new KFSMEvent("Undock");
			on_undock.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_undock.GoToStateOnEvent = st_ready;
			fsm.AddEvent(on_undock, st_docked, st_preattached);
		}

		void BuildRingObject()
		{
			RingObject = new GameObject();

			RingObject.AddComponent<Rigidbody>().mass = 0.005f;

			RingObject.transform.position = Ring.transform.position;
			RingObject.transform.rotation = Ring.transform.rotation;

			Ring.parent = RingObject.transform;

			RingObject.SetActive(true);

			// latest time to initialize this value
			extendDirection = Quaternion.Inverse(transform.rotation) * nodeTransform.forward;
		}

		void DestroyRingObject()
		{
			Ring.parent = originalRingParent;

			Ring.localPosition = originalRingLocalPosition;
			Ring.localRotation = originalRingLocalRotation;

			Destroy(RingObject);
		}

/*
was nu? das hier oder nicht? eigentlich wär mir fast lieber über den state zu gehen, weil...
dann wär's "gleicher"... und die followport sache muss ich ja auch tun...

-> ja ok, über den State gehen

		void PackRingObject()
		{
			RingObject.GetComponent<Rigidbody>().isKinematic = true;
			RingObject.GetComponent<Rigidbody>().detectCollisions = false;

			RingObject.transform.parent = transform;
		}

		void UnpackRingObject()
		{
			RingObject.GetComponent<Rigidbody>().isKinematic = false;
			RingObject.GetComponent<Rigidbody>().detectCollisions = true;

			RingObject.transform.parent = null;
		}
*/
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

			position = transform.InverseTransformDirection(diff) - ActiveJoint.anchor;
		}

		// calculate position and orientation for st_capture
		void CalculateCaptureJointRotationAndPosition(ModuleDockingPortEx port, out Quaternion rotation, out Vector3 position)
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

			rotation = qt * Quaternion.Inverse(qc);


			Vector3 diff = port.nodeTransform.position - nodeTransform.position;
			Vector3 difflp = Vector3.ProjectOnPlane(diff, transform.forward);

			position = -transform.InverseTransformDirection(difflp);
		}

		void ConfigureActiveJoint(ConfigurableJoint joint)
		{
			joint.xMotion = ConfigurableJointMotion.Limited;
			joint.yMotion = ConfigurableJointMotion.Limited;
			joint.zMotion = ConfigurableJointMotion.Limited;
			joint.angularXMotion = ConfigurableJointMotion.Limited;
			joint.angularYMotion = ConfigurableJointMotion.Limited;
			joint.angularZMotion = ConfigurableJointMotion.Limited;

			joint.xDrive = joint.yDrive = joint.zDrive =
			joint.angularXDrive = joint.angularYZDrive = 
				new JointDrive
				{
					positionSpring = 10000f,
					positionDamper = 0f,
					maximumForce = 10000f
				};
		}

		void DisableActiveJoint(ConfigurableJoint joint)
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

		// the ActiveJoint is the joint between the port and the RingObject
		ConfigurableJoint BuildActiveJoint()
		{
			ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
			joint.connectedBody = RingObject.GetComponent<Rigidbody>();

			joint.autoConfigureConnectedAnchor = false;
			joint.anchor = extendDirection * (maxExtensionLength * 0.5f);
			joint.targetPosition = extendDirection * -(maxExtensionLength * 0.5f);

			joint.rotationDriveMode = RotationDriveMode.XYAndZ;

			ConfigureActiveJoint(joint);

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
			RingObject.GetComponent<Rigidbody>().isKinematic = true;
			RingObject.GetComponent<Rigidbody>().detectCollisions = false;

			RingObject.transform.parent = port.transform;

			originalRingObjectLocalPosition = RingObject.transform.localPosition;
			originalRingObjectLocalRotation = RingObject.transform.localRotation;

		//	RingObject.transform.localPosition = Vector3.zero;
		//	RingObject.transform.localRotation = new Quaternion(0f, 0f, 1f, 0f);
// FEHLER, das da oben muss ich ändern, das mit der localPosition... hier probier ich mal was
_capturePositionA = RingObject.transform.localPosition;
_capturePositionB =
	//		RingObject.transform.position =
				port.Ring.position + port.Ring.transform.TransformDirection(correctionVector);
_capturePositionB = port.transform.InverseTransformPoint(_capturePositionB); // FEHLER, neu, dann kann ich's per localPosition setzen

			// snap local rotation
			float newY =
				Vector3.SignedAngle(RingObject.transform.TransformDirection(dockingOrientation), port.Ring.transform.TransformDirection(dockingOrientation), port.nodeTransform.forward);

			newY /= (360f / snapCount);
			newY = Mathf.Round(newY);
			newY *= (360f / snapCount);

			Quaternion targetLocalRotation = Quaternion.Inverse(otherPort.transform.rotation)
				* Quaternion.AngleAxis(-newY, port.nodeTransform.forward)
					* otherPort.Ring.transform.rotation * Quaternion.AngleAxis(180, dockingOrientation);

_captureRotationA = RingObject.transform.localRotation;
_captureRotationB =
//			RingObject.transform.localRotation =
				targetLocalRotation;
		}

		private void BuildCaptureJoint(ModuleDockingPortEx port)
		{
		// FEHLER, müsste doch schon gesetzt sein... auch beim Dock... aber gut...
			otherPort = port;
			dockedPartUId = otherPort.part.flightID;

			otherPort.otherPort = this;
			otherPort.dockedPartUId = part.flightID;

			// ActiveJoint
			DisableActiveJoint(ActiveJoint);

			// Ring
			SetCapturedRingPosition(port);

			_captureSlerp = 0f;

			// Joint
			ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
			joint.connectedBody = otherPort.GetComponent<Rigidbody>();

			joint.breakForce = joint.breakTorque = Mathf.Infinity;

			joint.xMotion = ConfigurableJointMotion.Free;
			joint.yMotion = ConfigurableJointMotion.Free;
			joint.zMotion = ConfigurableJointMotion.Free;
			joint.angularXMotion = ConfigurableJointMotion.Free;
			joint.angularYMotion = ConfigurableJointMotion.Free;
			joint.angularZMotion = ConfigurableJointMotion.Free;

			joint.rotationDriveMode = RotationDriveMode.Slerp;

			joint.xDrive = joint.yDrive = joint.zDrive =
			joint.slerpDrive =
				new JointDrive
				{
					positionSpring = 100f,
					positionDamper = 0.002f,
					maximumForce = 100f
				};

			CaptureJoint = joint;

// FEHLER, die Modelle sind oft so ein elender Schrott... unglaublich du... -> geht's so???
joint.anchor = joint.transform.InverseTransformPoint(nodeTransform.position);

iCapturePosition = -100;

			DockDistance = "-";
		}

		private void BuildCaptureJoint2()
		{
			CalculateCaptureJointRotationAndPosition(otherPort, out CaptureJointTargetRotation, out CaptureJointTargetPosition);

			_rotStep = 1f;
			_transStep = 1f;

			lastPreLatchDistance = (otherPort.nodeTransform.position - nodeTransform.position).magnitude;
		}

static bool n1 = true;

		private void DestroyCaptureJoint()
		{
			// RingObject
			RingObject.transform.localPosition = originalRingObjectLocalPosition;
			RingObject.transform.localRotation = originalRingObjectLocalRotation;

			RingObject.transform.parent = null;

			RingObject.GetComponent<Rigidbody>().isKinematic = false;
			RingObject.GetComponent<Rigidbody>().detectCollisions = true;

			// in rare cases, there is still a collider marked as part that can collide with us (when it had been counted as part of the otherPort)
			Collider[] colliders = RingObject.transform.GetComponentsInChildren<Collider>(true);
			CollisionManager.SetCollidersOnVessel(vessel, true, colliders);

			// ActiveJoint
			ConfigureActiveJoint(ActiveJoint);

// FEHLER, neu, klären ob's gut ist -> soll das "zurückziehen" nach dem uncapture schöner machen
if(n1)
{
			ActiveJoint.targetPosition = ActiveJoint.transform.InverseTransformPoint(RingObject.transform.position + RingObject.transform.rotation * ActiveJoint.connectedAnchor) - ActiveJoint.anchor;
			ActiveJoint.targetRotation = Quaternion.Inverse(ActiveJoint.transform.rotation) * RingObject.transform.rotation;

ActiveJointTargetPosition = ActiveJoint.targetPosition;
ActiveJointTargetRotation = ActiveJoint.targetRotation;
_pushStep = 1f;
}

			// Joint
			Destroy(CaptureJoint);
			CaptureJoint = null;
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

		[KSPEvent(guiActive = true, guiActiveUnfocused = false, guiName = "Release")]
		public void Release()
		{
			fsm.RunEvent(on_release);
		}

		[KSPEvent(guiActive = true, guiActiveUnfocused = false, guiName = "Perform Docking")]
		public void PerformDocking()
		{
			fsm.RunEvent(on_preparedocking);
		}

		////////////////////////////////////////
		// Update-Functions

		public void FixedUpdate()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				if(!vessel.packed)
				{

				if((fsm != null) && fsm.Started)
					fsm.FixedUpdateFSM();

				}

				if(vessel.packed && followOtherPort)
				{
					vessel.SetRotation(otherPort.part.transform.rotation * otherPortRelativeRotation, true);
					vessel.SetPosition(otherPort.part.transform.position + otherPort.part.transform.rotation * otherPortRelativePosition, false);
				//	vessel.IgnoreGForces(5);
				}
			}
		}

		public void Update()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				if(!vessel.packed)
				{

				if((fsm != null) && fsm.Started)
				{
					fsm.UpdateFSM();
					DockStatus = fsm.currentStateName;
				}

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
				if(!vessel.packed)
				{

				if((fsm != null) && fsm.Started)
					fsm.LateUpdateFSM();

				}

				if(RingObject)
					UpdatePistons();
			}
		}

		////////////////////////////////////////
		// Context Menu

		[KSPField(guiName = "DockingNode status", isPersistant = false, guiActive = true, guiActiveUnfocused = true, unfocusedRange = 20)]
		public string DockStatus = "";

		[KSPField(guiName = "DockingNode distance", isPersistant = false, guiActive = true)]
		public string DockDistance;

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

		[KSPEvent(guiActive = true, guiActiveUnfocused = false, guiName = "Extend Ring")]
		public void ExtendRing()
		{
			fsm.RunEvent(on_extend);
		}
	
		[KSPEvent(guiActive = true, guiActiveUnfocused = false, guiName = "Retract Ring")]
		public void RetractRing()
		{
			fsm.RunEvent(on_retract);
		}

		public DockedVesselInfo vesselInfo;

		public void DockToVessel(ModuleDockingPortEx port)
		{
Vector3 position1, position2;
Transform tf; FlightCamera.TargetMode tm;

			StartCoroutine(ahi(
				position1 = part.transform.InverseTransformPoint(FlightCamera.fetch.GetPivot().position),
				position2 = part.transform.InverseTransformPoint(FlightCamera.fetch.GetCameraTransform().position),
				part.transform.InverseTransformPoint(FlightCamera.fetch.GetCameraTransform().position),
				tm = FlightCamera.fetch.targetMode, tf = FlightCamera.fetch.Target));

			Debug.Log("Docking to vessel " + port.vessel.GetDisplayName(), gameObject);

			otherPort = port;
			dockedPartUId = otherPort.part.flightID;

			otherPort.otherPort = this;
			otherPort.dockedPartUId = part.flightID;

			vesselInfo = new DockedVesselInfo();
			vesselInfo.name = vessel.vesselName;
			vesselInfo.vesselType = vessel.vesselType;
			vesselInfo.rootPartUId = vessel.rootPart.flightID;

			otherPort.vesselInfo = new DockedVesselInfo();
			otherPort.vesselInfo.name = otherPort.vessel.vesselName;
			otherPort.vesselInfo.vesselType = otherPort.vessel.vesselType;
			otherPort.vesselInfo.rootPartUId = otherPort.vessel.rootPart.flightID;

			uint data = vessel.persistentId;
			uint data2 = otherPort.vessel.persistentId;

			Vessel oldvessel = vessel;

			GameEvents.onVesselDocking.Fire(data, data2);
			GameEvents.onActiveJointNeedUpdate.Fire(otherPort.vessel);
			GameEvents.onActiveJointNeedUpdate.Fire(vessel);

			otherPort.vessel.SetRotation(otherPort.vessel.transform.rotation);
			vessel.SetRotation(Quaternion.FromToRotation(nodeTransform.forward, -otherPort.nodeTransform.forward) * vessel.transform.rotation);
			vessel.SetPosition(vessel.transform.position - (nodeTransform.position - otherPort.nodeTransform.position), usePristineCoords: true);
			vessel.IgnoreGForces(10);
			part.Couple(otherPort.part);

			GameEvents.onVesselPersistentIdChanged.Fire(data, data2);

			if(oldvessel == FlightGlobals.ActiveVessel)
			{
				FlightGlobals.ForceSetActiveVessel(vessel);
				FlightInputHandler.SetNeutralControls();
			}
			else if(vessel == FlightGlobals.ActiveVessel)
			{
				vessel.MakeActive();
				FlightInputHandler.SetNeutralControls();
			}

ahiSofort(position1, position2, position2, tm, tf);

			for(int i = 0; i < vessel.parts.Count; i++)
			{
				FlightGlobals.PersistentLoadedPartIds.Add(vessel.parts[i].persistentId, vessel.parts[i]);
				if(vessel.parts[i].protoPartSnapshot == null)
					continue;
				FlightGlobals.PersistentUnloadedPartIds.Add(vessel.parts[i].protoPartSnapshot.persistentId, vessel.parts[i].protoPartSnapshot);
			}

			GameEvents.onVesselWasModified.Fire(vessel);
			GameEvents.onDockingComplete.Fire(new GameEvents.FromToAction<Part, Part>(part, otherPort.part));

ahiSofort(position1, position2, position2, tm, tf);
		}

static float undockEjectionForce = 1f; // FEHLER, default ist 10f bei "normalem" Port -> 0.1f war's bei mir, aber reicht vielleicht nicht

		void DeactivateColliders(Vessel v)
		{
			Collider[] colliders = part.transform.GetComponentsInChildren<Collider>(true);
			CollisionManager.SetCollidersOnVessel(v, true, colliders);
		}

		private void DoUndock()
		{
Vector3 position1, position2;
Transform tf; FlightCamera.TargetMode tm;

			StartCoroutine(ahi(
				position1 = part.transform.InverseTransformPoint(FlightCamera.fetch.GetPivot().position),
				position2 = part.transform.InverseTransformPoint(FlightCamera.fetch.GetCameraTransform().position),
				part.transform.InverseTransformPoint(FlightCamera.fetch.GetCameraTransform().position),
				tm = FlightCamera.fetch.targetMode, tf = FlightCamera.fetch.Target));

			Part parent = part.parent;

			if(DockStatus == "Attached")
				part.decouple();
			else
				part.Undock(vesselInfo);

ahiSofort(position1, position2, position2, tm, tf);

otherPort.DeactivateColliders(vessel);
DeactivateColliders(otherPort.vessel);


			ConfigurableJoint j = part.gameObject.AddComponent<ConfigurableJoint>();
			j.connectedBody = otherPort.part.rb;
			j.axis = j.transform.InverseTransformDirection(nodeTransform.forward);
			j.xMotion = ConfigurableJointMotion.Free;
			j.yMotion = ConfigurableJointMotion.Free;
			j.zMotion = ConfigurableJointMotion.Free;
JointDrive strf = new JointDrive();
			strf.maximumForce = 1000000f; strf.positionSpring = 1000000f;

j.yDrive = j.zDrive = strf;
	j.xDrive = strf;
			j.angularXMotion = j.angularYMotion = j.angularZMotion = ConfigurableJointMotion.Free;

j.angularXDrive = j.angularYZDrive = strf;

//StartCoroutine(killAngVel(vessel, 50, parent.rb.angularVelocity));
StartCoroutine(killAngVel2(j, 50,
	Mathf.Min(vessel.GetTotalMass(), otherPort.vessel.GetTotalMass())
	
	));

/*
if(undockEjectionForce > 0.001f)
			{
			part.AddForce(nodeTransform.forward * ((0f - undockEjectionForce) * 0.5f));
			parent.AddForce(nodeTransform.forward * (undockEjectionForce * 0.5f));
			}
*/

			otherPort.fsm.RunEvent(otherPort.on_undock);
			fsm.RunEvent(on_undock);

ahiSofort(position1, position2, position2, tm, tf);

/* -> sowas noch einbauen dann...
 * 
			if(undockPreAttached)
			{
				Decouple();
				fsm.RunEvent(on_undock);
				if(otherNode != null)
					otherNode.OnOtherNodeUndock();
				undockPreAttached = false;
				return;
			}
*/
		}

static bool dozeroinstead = true;
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

		void ahiSofort(Vector3 position, Vector3 position2, Vector3 position3, FlightCamera.TargetMode m, Transform p)
		{
			FlightCamera.fetch.SetTarget(p, true, m);

			FlightCamera.fetch.GetPivot().position = part.transform.TransformPoint(position);
			FlightCamera.fetch.SetCamCoordsFromPosition(part.transform.TransformPoint(position2));
			FlightCamera.fetch.GetCameraTransform().position = part.transform.TransformPoint(position3);
		}

static int waitframes = 1; // FEHLER, nur, damit wir sicher keine Kollisionen haben zum Testen

		IEnumerator ahi(Vector3 position, Vector3 position2, Vector3 position3, FlightCamera.TargetMode m, Transform p)
		{
// FEHLER, so lange müsste man nie warten
			for(int i = 0; i < waitframes; i++)
				yield return new WaitForEndOfFrame();

			ahiSofort(position, position2, position3, m, p);
		}

		[KSPEvent(guiActive = true, guiActiveUnfocused = true, externalToEVAOnly = true, unfocusedRange = 2f, guiName = "#autoLOC_6001445")]
		public void Undock()
		{
			Vessel oldvessel = vessel;
			uint referenceTransformId = vessel.referenceTransformId;

			if(part.parent == otherPort.part)
				DoUndock();
			else
				otherPort.DoUndock();

			if(oldvessel == FlightGlobals.ActiveVessel)
			{
				if(vessel[referenceTransformId] == null)
					StartCoroutine(WaitAndSwitchFocus());
			}
		}

		public IEnumerator WaitAndSwitchFocus()
		{
			yield return null;

Vector3 position1, position2;
Transform tf; FlightCamera.TargetMode tm;

position1 = part.transform.InverseTransformPoint(FlightCamera.fetch.GetPivot().position);
position2 = part.transform.InverseTransformPoint(FlightCamera.fetch.GetCameraTransform().position);
tm = FlightCamera.fetch.targetMode; tf = FlightCamera.fetch.Target;

			FlightGlobals.ForceSetActiveVessel(vessel);
			FlightInputHandler.SetNeutralControls();

ahiSofort(position1, position2, position2, tm, tf);
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "#autoLOC_236028")]
		public void EnableXFeed()
		{
			Events["EnableXFeed"].active = false;
			Events["DisableXFeed"].active = true;
			bool fuelCrossFeed = part.fuelCrossFeed;
			part.fuelCrossFeed = (crossfeed = true);
			if(fuelCrossFeed != crossfeed)
				GameEvents.onPartCrossfeedStateChange.Fire(base.part);
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "#autoLOC_236030")]
		public void DisableXFeed()
		{
			Events["EnableXFeed"].active = true;
			Events["DisableXFeed"].active = false;
			bool fuelCrossFeed = base.part.fuelCrossFeed;
			base.part.fuelCrossFeed = (crossfeed = false);
			if(fuelCrossFeed != crossfeed)
				GameEvents.onPartCrossfeedStateChange.Fire(base.part);
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
		// ITargetable

		public Transform GetTransform()
		{
			return nodeTransform;
		}

		public Vector3 GetObtVelocity()
		{
			return base.vessel.obt_velocity;
		}

		public Vector3 GetSrfVelocity()
		{
			return base.vessel.srf_velocity;
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
			return "name fehlt noch"; // FEHLER, einbauen
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

		////////////////////////////////////////
		// Debug

		private MultiLineDrawer ld;

		private String[] astrDebug;
		private int istrDebugPos;

		private void DebugInit()
		{
			ld = new MultiLineDrawer();
			ld.Create(null);

			astrDebug = new String[10240];
			istrDebugPos = 0;
		}

		private void DebugString(String s)
		{
			astrDebug[istrDebugPos] = s;
			istrDebugPos = (istrDebugPos + 1) % 10240;
		}

		private void DrawPointer(int idx, Vector3 p_vector)
		{
			ld.Draw(idx, Vector3.zero, p_vector);
		}

		private void DrawRelative(int idx, Vector3 p_from, Vector3 p_vector)
		{
			ld.Draw(idx, p_from, p_from + p_vector);
		}

		private void DrawAxis(int idx, Transform p_transform, Vector3 p_vector, bool p_relative, Vector3 p_off)
		{
			ld.Draw(idx, p_transform.position + p_off, p_transform.position + p_off
				+ (p_relative ? p_transform.TransformDirection(p_vector) : p_vector));
		}

		private void DrawAxis(int idx, Transform p_transform, Vector3 p_vector, bool p_relative)
		{ DrawAxis(idx, p_transform, p_vector, p_relative, Vector3.zero); }
	}
}
