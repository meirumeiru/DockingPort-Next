using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KAS
{
	public class DockingPortEx : KASModuleHarpoon
	{
		private ModuleDockingNode dock = null;

		KFSMEvent on_enable2 = null, on_disable2 = null;

		public void Start()
		{
//			if(!HighLogic.LoadedSceneIsFlight)
//				return;

			try
			{
				dock = part.FindModuleImplementing<ModuleDockingNode>();

				/*
				this.on_undock = new KFSMEvent("Undock");
				this.on_undock.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
				this.on_undock.GoToStateOnEvent = this.st_disengage;
				this.on_undock.OnEvent = delegate
				{
					if(this.deployAnimator != null && this.setAnimWrite)
					{
						this.deployAnimator.SetUIWrite(true);
					}
					if(this.animUndockOn && this.deployAnimator)
					{
						this.deployAnimator.Events["Toggle"].active = true;
					}
					this.on_undock.GoToStateOnEvent = ((!this.otherNode) ? this.st_ready : this.st_disengage);
				};
				this.fsm.AddEvent(this.on_undock, new KFSMState[]
				{
					this.st_docked_docker,
					this.st_docked_dockee,
					this.st_preattached,
					this.st_docker_sameVessel
				});	*/




			}
			catch(Exception)
			{}
		}

		KFSMCallback on_preattachDecouple = null;
		public void preattachDecoupleCallback()
		{
			Debug.Log(String.Format("preattachDecoupleCallback"));
			if(on_preattachDecouple != null)
				on_preattachDecouple();
		}
		KFSMCallback on_capture = null;
		public void captureCallback()
		{
			Debug.Log(String.Format("captureCallback"));
			if(on_capture != null)
				on_capture();
		}
		KFSMCallback on_capture_dockee = null;
		public void capture_dockeeCallback()
		{
			Debug.Log(String.Format("capture_dockeeCallback"));
			if(on_capture_dockee != null)
				on_capture_dockee();
		}
		KFSMCallback on_capture_docker = null;
		public void capture_dockerCallback()
		{
			Debug.Log(String.Format("capture_dockerCallback"));
			if(on_capture_docker != null)
				on_capture_docker();
		}
		KFSMCallback on_capture_docker_sameVessel = null;
		public void capture_docker_sameVesselCallback()
		{
			Debug.Log(String.Format("capture_docker_sameVesselCallback"));
			if(on_capture_docker_sameVessel != null)
				on_capture_docker_sameVessel();
		}
		KFSMCallback on_decouple = null;
		public void decoupleCallback()
		{
			Debug.Log(String.Format("decoupleCallback"));
			if(on_decouple != null)
				on_decouple();
		}
		KFSMCallback on_disable = null;
		public void disableCallback()
		{
			Debug.Log(String.Format("disableCallback"));
			if(on_disable != null)
				on_disable();
		}
		KFSMCallback on_enable = null;
		public void enableCallback()
		{
			Debug.Log(String.Format("enableCallback"));
			if(on_enable != null)
				on_enable();
		}
		KFSMCallback on_nodeApproach = null;
		public void nodeApproachCallback()
		{
			Debug.Log(String.Format("nodeApproachCallback"));
			if(on_nodeApproach != null)
				on_nodeApproach();
		}
		KFSMCallback on_nodeDistance = null;
		public void nodeDistanceCallback()
		{
			Debug.Log(String.Format("nodeDistanceCallback"));
			if(on_nodeDistance != null)
				on_nodeDistance();

/*			Debug.Log("d1 later in");
			EnumAllProperties(d1);
			Debug.Log("d1 later out");
			Debug.Log("d1.later post in");
			EnumAllProperties(d1.part);
			Debug.Log("d1.later post out");

			Debug.Log("d2 later in");
			EnumAllProperties(d2);
			Debug.Log("d2 later out");
			Debug.Log("d2.later post in");
			EnumAllProperties(d2.part);
			Debug.Log("d2.later post out");*/
		}
		KFSMCallback on_sameVessel_disconnect = null;
		public void sameVessel_disconnectCallback()
		{
			Debug.Log(String.Format("sameVessel_disconnectCallback"));
			if(on_sameVessel_disconnect != null)
				on_sameVessel_disconnect();
		}
		KFSMCallback on_undock = null;
		public void undockCallback()
		{
			Debug.Log(String.Format("undockCallback"));
			if(on_undock != null)
				on_undock();
		}
	
		public void Update()
		{
			if(dock.state != "PreAttached")
			{
				try
				{
					Events["ContextMenuDecoupleAndDock"].guiActive = false;
					Events["ContextMenuDecoupleAndDock"].guiActiveUnfocused = false;
					Events["ContextMenuDecoupleAndDock"].active = false;
				}
				catch(Exception)
				{
					//	Debug.Log(String.Format("[ModuleWeldablePart] Error {0} in OnStart", ex.Message));
				}
			}
		}

		public override void OnFixedUpdate()
		{
			base.OnFixedUpdate();
			Update();
		}

		public override void OnUpdate()
		{
			base.OnUpdate();
			Update();
		}

		[KSPEvent(name = "ContextMenuDecoupleAndDock", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "Decouple and Dock")]
		public void ContextMenuDecoupleAndDock()
		{
			// undock

			ModuleDockingNode dock2 = dock.otherNode;

			dock.Decouple();
			
			// entfernen (scheinbar funktioniert es nicht ohne)
			dock.fsm.RunEvent(dock.on_nodeDistance);
			dock2.fsm.RunEvent(dock2.on_nodeDistance);

			// dock

			dock.DockToSameVessel(dock2);
		//	dock.DockToVessel(dock2);
		}

		int undocktype = 0;

		[KSPEvent(name = "ContextMenuUnDockType", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "undocktype: 0")]
		public void ContextMenuUnDockType()
		{
			undocktype = (undocktype + 1) % 8;

			Events["ContextMenuUnDockType"].guiName = "undocktype: " + undocktype.ToString();
		}

		[KSPEvent(name = "ContextMenuDecoupleAndDock2", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "Decouple and Dock2")]
		public void ContextMenuDecoupleAndDock2()
		{
			// undock

			ModuleDockingNode dock2 = dock.otherNode;

			bool bE = false;

			if(undocktype > 4)
				dock.Undock();
			else
			{
				dock.Decouple();

				if(bE)
				{
					dock.fsm.RunEvent(dock.on_undock);
					dock2.fsm.RunEvent(dock2.on_undock);
				}
			}

			// entfernen (scheinbar funktioniert es nicht ohne)

			dock.fsm.RunEvent(dock.on_nodeDistance);
			dock2.fsm.RunEvent(dock2.on_nodeDistance);

			// dock

			if((undocktype % 2) == 0)
				dock.DockToVessel(dock2);
			else
				dock.DockToSameVessel(dock2);

			if((undocktype % 4) > 1)
			{
				if((undocktype % 2) == 0)
				{
					dock.Undock();

					dock.FixedUpdate();
					dock.Update();

					dock2.FixedUpdate();
					dock2.Update();

					dock.fsm.RunEvent(dock.on_nodeDistance);
					dock2.fsm.RunEvent(dock2.on_nodeDistance);

					dock.DockToVessel(dock2);
				}
				else
				{
					dock.UndockSameVessel();

					dock.FixedUpdate();
					dock.Update();

					dock2.FixedUpdate();
					dock2.Update();

					dock.fsm.RunEvent(dock.on_nodeDistance);
					dock2.fsm.RunEvent(dock2.on_nodeDistance);

					dock.DockToSameVessel(dock2);
				}
			}
		}

		private IEnumerator<YieldInstruction> WaitAndInitFixedAttach()
		{
			yield return new WaitForEndOfFrame();

			//					dock.fsm.RunEvent(dock.on_nodeDistance);
			//				dock2.fsm.RunEvent(dock2.on_nodeDistance);

			dock = dock;
		}

		static ModuleDockingNode d1;
		static ModuleDockingNode d2;

		static bool bRepeated = false;

		private IEnumerator<YieldInstruction> WaitAndInitFixedAttach2()
		{
			yield return new WaitForFixedUpdate();

			ModuleDockingNode dA = d1;
			ModuleDockingNode dB = d2;

			d1.fsm.RunEvent(d1.on_nodeDistance);
			d2.fsm.RunEvent(d2.on_nodeDistance);

			d1.FixedUpdate();
			d2.FixedUpdate();

			d1.Update();
			d2.Update();

			d1.LateUpdate();
			d2.LateUpdate();

			yield return new WaitForFixedUpdate();

//			d1.DockToVessel(d2);

			//	d1.DockToVessel(d2);

			// machen wir mal was anderes... nur so zum Spass...

			int v1 = 0, v2 = 0;

			while(d1.vessel != FlightGlobals.VesselsLoaded[v1]) ++v1;
			while(d2.vessel != FlightGlobals.VesselsLoaded[v2]) ++v2;

			int p1 = 0, p2 = 0;

			while(d1.GetInstanceID() != FlightGlobals.VesselsLoaded[v1].dockingPorts[p1].GetInstanceID()) ++p1;
			while(d2.GetInstanceID() != FlightGlobals.VesselsLoaded[v2].dockingPorts[p2].GetInstanceID()) ++p2;

			if(d1.vessel.vesselType < d2.vessel.vesselType)
			{ int t = v1; v1 = v2; v2 = t; t = p1; p1 = p2; p2 = t; }
			else if(d1.vessel.vesselType == d2.vessel.vesselType)
			{
				if(d1.vessel.GetTotalMass() < d2.vessel.GetTotalMass())
				{ int t = v1; v1 = v2; v2 = t; t = p1; p1 = p2; p2 = t; }
				else if(d1.vessel.GetTotalMass() == d2.vessel.GetTotalMass())
				{
					if(d1.vessel.id.CompareTo(d2.vessel.id) < 0)
					{ int t = v1; v1 = v2; v2 = t; t = p1; p1 = p2; p2 = t; }
				}
			}

			FlightGlobals.SetActiveVessel(FlightGlobals.VesselsLoaded[v1]);
			FlightInputHandler.ResumeVesselCtrlState(FlightGlobals.VesselsLoaded[v1]);

			((ModuleDockingNode)FlightGlobals.VesselsLoaded[v1].dockingPorts[p1]).DockToVessel(
				(ModuleDockingNode)FlightGlobals.VesselsLoaded[v2].dockingPorts[p2]);
		}

		public void EnumAllProperties(object s)
		{
//			foreach(var p in s.GetType().GetProperties().Where(p => p.GetGetMethod().GetParameters().Count() == 0))
//			{
//				Debug.Log(String.Format("{0} = {1}", p.Name, p.GetValue(s, null).ToString()));
//			}
			ModuleDockingNode q = (ModuleDockingNode)s;
			q = q;
		}


		[KSPEvent(name = "ContextMenuUndockF", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "Undock forced")]
		public void ContextMenuUndockF()
		{
	//		ContextMenuVermienen();

			d1 = dock;
			d2 = dock.otherNode;

			if(dock.part.parent == dock.otherNode.part)
			{
				d1 = dock;
				d2 = dock.otherNode;
			}
			else
			{
				d2 = dock;
				d1 = dock.otherNode;
			}

			on_nodeDistance = d1.on_nodeDistance.OnEvent;
			d1.on_nodeDistance.OnEvent = new KFSMCallback(nodeDistanceCallback);

	/*		Debug.Log("d1 pre in");
			EnumAllProperties(d1);
			Debug.Log("d1 pre out");
			Debug.Log("d1.part pre in");
			EnumAllProperties(d1.part);
			Debug.Log("d1.part pre out");

			Debug.Log("d2 pre in");
			EnumAllProperties(d2);
			Debug.Log("d2 pre out");
			Debug.Log("d2.part pre in");
			EnumAllProperties(d2.part);
			Debug.Log("d2.part pre out");*/

			d1.Decouple();

	/*		Debug.Log("d1 post in");
			EnumAllProperties(d1);
			Debug.Log("d1 post out");
			Debug.Log("d1.part post in");
			EnumAllProperties(d1.part);
			Debug.Log("d1.part post out");

			Debug.Log("d2 post in");
			EnumAllProperties(d2);
			Debug.Log("d2 post out");
			Debug.Log("d2.part post in");
			EnumAllProperties(d2.part);
			Debug.Log("d2.part post out");*/

			return;
	
			KAS.KAS_Shared.RemoveAttachJointBetween(d1.part, d2.part);
			KAS.KAS_Shared.RemoveFixedJointBetween(d1.part, d2.part);
			KAS.KAS_Shared.RemoveHingeJointBetween(d1.part, d2.part);

			return;

	//		d1.vessel.rootPart;

			d1.Decouple();
/*
			d1.vessel.Clean;
			d1.vessel.Connection;
		//	d1.vessel.GoOffRails -> intu simulated orbit, onRais -> into propagated orbit
			d1.vessel.KillPermanentGroundContact;
			d1.vessel.UpdateAcceleration;
			d1.vessel.UpdateCaches;
			d1.vessel.UpdateLandedSplashed;
			d1.vessel.UpdatePosVel;
			d1.vessel.UpdateResourceSets;
			d1.vessel.UpdateResourceSetsIfDirty;
			d1.vessel.UpdateVesselModuleActivation;*/

/*
			dock.FixedUpdate();
			d2.FixedUpdate();

			dock.Update();
			d2.Update();

			dock.LateUpdate();
			d2.LateUpdate();
*/
	//		GameEvents.onNewVesselCreated;
	//		GameEvents.onPartUndock;
	//		GameEvents.onUndock.Fire;
	//		GameEvents.onVesselChange;
	//		GameEvents.onVesselPartCountChanged;

			FlightInputHandler.ResumeVesselCtrlState(d1.vessel);
			FlightInputHandler.ResumeVesselCtrlState(d2.vessel);

			GameEvents.onVesselWasModified.Fire(d1.vessel);
			GameEvents.onVesselWasModified.Fire(d2.vessel);

			GameEvents.onVesselPartCountChanged.Fire(d1.vessel);
			GameEvents.onVesselStandardModification.Fire(d1.vessel);

			GameEvents.onVesselPartCountChanged.Fire(d2.vessel);
			GameEvents.onVesselStandardModification.Fire(d2.vessel);

			StartCoroutine(WaitAndInitFixedAttach2());

//			dock.fsm.RunEvent(dock.on_undock);
//			dock2.fsm.RunEvent(dock2.on_undock);

				// weiss nicht ob das oben nötig ist... aber -> nach dem ReDock muss man die Szene neu laden... super komisch
		}

		bool Undock(Part Host, Part Target)
		{
			if((Host.attachJoint.Host == Host) && (Host.attachJoint.Target == Target))
			{
				Host.attachJoint.DestroyJoint();

				// Host.attachJoint.Child = null;
				// Host.attachJoint.Host = null;
				// Host.attachJoint.Parent = null;
				// Host.attachJoint.Target = null;

				Host.attachJoint.OnDestroy();

				return true;
			}
			else if((Target.attachJoint.Host == Target) && (Target.attachJoint.Target == Host))
			{
				Target.attachJoint.DestroyJoint();

				// Target.attachJoint.Child = null;
				// Target.attachJoint.Host = null;
				// Target.attachJoint.Parent = null;
				// Target.attachJoint.Target = null;

				Target.attachJoint.OnDestroy();

				return true;
			}
			else
				return false;
		}

		[KSPEvent(name = "ContextMenuUndockFs", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "-- Undock Test --")]
		public void ContextMenuUndockFs()
		{
			// das hier macht wohl ein... decouple and dock... obwohl ich's rausspringen lasse und es kommt
			// gleich zurück... aber, das geht offenbar...

			// mal sehen, ob ich die "main-second" Sache drehen kann... das wär echt super...
			// das docking auf Zuruf wird sicher noch interessant... aber, das kommt schon noch...

			// das ist das normale Decouple vom ModuleDockingPort... mal sehen ob's geht

			Part attachedPart = dock.referenceNode.attachedPart;
			if (dock.referenceNode.attachedPart == dock.part.parent)
			{
				dock.part.decouple(0f);
			}
			else
			{
				dock.referenceNode.attachedPart.decouple(0f);
			}

			// geht, aber ich brauch keine Force
		//	dock.part.AddForce(dock.nodeTransform.forward * (-dock.undockEjectionForce * 0.5f));
		//	attachedPart.AddForce(dock.nodeTransform.forward * (dock.undockEjectionForce * 0.5f));

			KFSMCallback e0 = dock.on_undock.OnEvent;
			KFSMCallback e1 = null;

			// wir schreiben die Funktionen um... -> damit müssten die Ports sofort wieder ready sein...
				dock.on_undock.OnEvent = delegate
				{
					// if's lass ich weg, ist mir egal
					dock.on_undock.GoToStateOnEvent = dock.st_ready; // direkt wieder ready
				};

				if(dock.otherNode)
				{
					e1 = dock.otherNode.on_undock.OnEvent;

					// das da oben ist Standard, ich definier das jetzt mal um....
					dock.otherNode.on_undock.OnEvent = delegate
					{
						// if's lass ich weg, ist mir egal
						dock.otherNode.on_undock.GoToStateOnEvent = dock.otherNode.st_ready; // direkt wieder ready
					};
				}

			dock.fsm.RunEvent(dock.on_undock);

			dock.on_undock.OnEvent = e0;

			if (dock.otherNode)
			{
				dock.otherNode.OnOtherNodeUndock();

				dock.otherNode.on_undock.OnEvent = e1;
			}

			return;





			if(dock.otherNode)
			{
				if(!Undock(dock.part, dock.otherNode.part))
					return;

				// sollte undocked sein... aber nicht wirklich... die Teils gehören noch zum gleichen Schiff... das ist aber ok für uns... genau das wollen wir ja

				GameEvents.onVesselWasModified.Fire(dock.part.vessel);




//uint LetThereBeKerbal(ProtoCrewMember protocrew)
if(false)
{
				// das hier lässt Bill Kerman's regnen... :-) echt

	ProtoCrewMember protocrew = HighLogic.CurrentGame.CrewRoster[1];

	var body = vessel.mainBody;
	var flight = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
	var crew = new ProtoCrewMember[1];
	crew[0] = protocrew;
	var parts = new ConfigNode[1];
	parts[0] = ProtoVessel.CreatePartNode("kerbalEVA", flight, crew);
	var extra = new ConfigNode[0];
	var orbit = Orbit.CreateRandomOrbitAround(body);
	var vesselConfig = ProtoVessel.CreateVesselNode(protocrew.name, VesselType.EVA, orbit, 0, parts, extra);
	var position = part.transform.position;
	position += (position-body.position).normalized * 5.0;
	vesselConfig.SetValue("sit", Vessel.Situations.LANDED.ToString());
	vesselConfig.SetValue("landed", true.ToString());
	vesselConfig.SetValue("lat", (body.GetLatitude(position)).ToString());
	vesselConfig.SetValue("lon", (body.GetLongitude(position)).ToString());
	vesselConfig.SetValue("alt", (body.GetAltitude(position)).ToString());
	var protoVessel = HighLogic.CurrentGame.AddVessel(vesselConfig);
//	return flight;
}

				{
	var body = vessel.mainBody;
	uint flightID = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
	ConfigNode[] partNodes = new ConfigNode[1];
	partNodes[0] = ProtoVessel.CreatePartNode("dockingPort3", flightID, null);
	ConfigNode[] additionalNodes = new ConfigNode[0];
	var orbit = Orbit.CreateRandomOrbitAround(body);
orbit = vessel.orbit;
	ConfigNode protoVesselNode = ProtoVessel.CreateVesselNode("Schrott", VesselType.Debris, orbit, 0, partNodes, additionalNodes);
	var position = part.transform.position;
	position += (position - body.position).normalized * 5.0;
	protoVesselNode.SetValue("lat", (body.GetLatitude(position)).ToString());
	protoVesselNode.SetValue("lon", (body.GetLongitude(position)).ToString());
	protoVesselNode.SetValue("alt", (body.GetAltitude(position)).ToString());

		// ohne die zwei da fliegt das Zeug echt im Orbit umher... das würgt also irgendwas hin, was das obere falsch gemacht hat oder so
	protoVesselNode.SetValue("sit", Vessel.Situations.LANDED.ToString());
	protoVesselNode.SetValue("landed", true.ToString());

	ProtoVessel protoVessel = HighLogic.CurrentGame.AddVessel(protoVesselNode);
				}

				ConfigNode cn = ConfigNode.CreateConfigFromObject(dock);

				ConfigNode cn2 = new ConfigNode();
				dock.OnSave(cn2);

				string scn = cn.ToString();

				string scn2 = cn2.ToString();


				cn = ConfigNode.CreateConfigFromObject(dock.part);

				cn2 = new ConfigNode();
				dock.part.OnSave(cn2);

				scn = cn.ToString();

				scn2 = cn2.ToString();

/*
				ShipConstruction.
				Proto

				dock.part.
				FlightGlobals.VesselsUnloaded[0].Load();

				vessel.sa
				dock.part.OnSave(

gut, Schrott erzeugen können wir... können wir nun auch diese abgekoppelten Dingens zu einem Vessel machen

			// na gut, ich bau ein Vessel... mal sehen
/*
				dock.part.vessel.

				uint flightID = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);

				ProtoVessel.CreateVesselNode

				ProtoVessel.CreatePartNode(
				ConfigNode c = new ConfigNode();
				c.

				dock.otherNode.part

				HighLogic.CurrentGame.AddVessel(

				FlightGlobals.AddVessel(Vessel.


/*				if(dock.part.vessel == dock.otherNode.part.vessel
        && attachMode.Docked && dockedAttachModule == otherAttachModule
        && otherAttachModule.attachMode.Docked && otherAttachModule.dockedAttachModule == this
        && vesselInfo != null && otherAttachModule.vesselInfo != null) {
      KAS_Shared.DebugWarning("DockTo(Core) Parts already docked, nothing to do at all");
      return;
    }*/

/*
				// Save vessel Info
				DockedVesselInfo vesselInfo = new DockedVesselInfo();
				vesselInfo.name = vessel.vesselName;
    vesselInfo.vesselType = vessel.vesselType;
    vesselInfo.rootPartUId = vessel.rootPart.flightID;
    dockedAttachModule = otherAttachModule;
    dockedPartID = otherAttachModule.part.flightID.ToString();

    otherAttachModule.vesselInfo = new DockedVesselInfo();
    otherAttachModule.vesselInfo.name = otherAttachModule.vessel.vesselName;
    otherAttachModule.vesselInfo.vesselType = otherAttachModule.vessel.vesselType;
    otherAttachModule.vesselInfo.rootPartUId = otherAttachModule.vessel.rootPart.flightID;
    otherAttachModule.dockedAttachModule = this;
    otherAttachModule.dockedPartID = part.flightID.ToString();

    // Set reference
    attachMode.Docked = true;
    otherAttachModule.attachMode.Docked = true;

    // Stop if already docked
    if (otherAttachModule.part.parent == part || part.parent == otherAttachModule.part) {
      KAS_Shared.DebugWarning("DockTo(Core) Parts already docked, nothing more to do");
      return;
    }

    // This results in a somewhat wrong state, but it's better to not make it even more wrong.
    if (otherAttachModule.part.vessel == part.vessel) {
      KAS_Shared.DebugWarning("DockTo(Core) BUG: Parts belong to the same vessel, doing nothing");
      return;
    }

    // Reset vessels position and rotation for returning all parts to their original position and
    // rotation before coupling
    vessel.SetPosition(vessel.transform.position, true);
    vessel.SetRotation(vessel.transform.rotation);
    otherAttachModule.vessel.SetPosition(otherAttachModule.vessel.transform.position, true);
    otherAttachModule.vessel.SetRotation(otherAttachModule.vessel.transform.rotation);
          
    // Couple depending of mass

    Vessel dominantVessel = GetDominantVessel(this.vessel, otherAttachModule.vessel);

    if (forceDominant == this.vessel || forceDominant == otherAttachModule.vessel) {
      dominantVessel = forceDominant;
    }

    KAS_Shared.DebugLog(string.Format("DockTo(Core) Master vessel is {0}",
                                      dominantVessel.vesselName));
          
    if (dominantVessel == this.vessel) {
      KAS_Shared.DebugLog(string.Format("DockTo(Core) Docking {0} from {1} with {2} from {3}",
                                        otherAttachModule.part.partInfo.title,
                                        otherAttachModule.vessel.vesselName,
                                        part.partInfo.title,
                                        vessel.vesselName));
      if (FlightGlobals.ActiveVessel == otherAttachModule.part.vessel) {
        KAS_Shared.DebugLog(string.Format("DockTo(Core) Switching focus to {0}",
                                          this.part.vessel.vesselName));
        FlightGlobals.ForceSetActiveVessel(this.part.vessel);
        FlightInputHandler.ResumeVesselCtrlState(this.part.vessel);
      }
      otherAttachModule.part.Couple(this.part);
    } else {
      KAS_Shared.DebugLog(string.Format("DockTo(Core) Docking {0} from {1} with {2} from {3}",
                                        part.partInfo.title,
                                        vessel.vesselName,
                                        otherAttachModule.part.partInfo.title,
                                        otherAttachModule.vessel.vesselName));
      if (FlightGlobals.ActiveVessel == part.vessel) {
        KAS_Shared.DebugLog(string.Format("DockTo(Core) Switching focus to {0}",
                                          otherAttachModule.part.vessel.vesselName));
        FlightGlobals.ForceSetActiveVessel(otherAttachModule.part.vessel);
        FlightInputHandler.ResumeVesselCtrlState(otherAttachModule.part.vessel);
      }
      part.Couple(otherAttachModule.part);
    }

    GameEvents.onVesselWasModified.Fire(this.part.vessel);
  }

  private Vessel GetDominantVessel(Vessel v1, Vessel v2) {
    // Check 1 - Dominant vessel will be the higher type
    if (v1.vesselType > v2.vesselType) {
      return v1;
    }
    if (v1.vesselType < v2.vesselType) {
      return v2;
    }

    // Check 2- If type are the same, dominant vessel will be the heaviest
    float diffMass = Mathf.Abs((v1.GetTotalMass() - v2.GetTotalMass()));
    if (diffMass >= 0.01f) {
      return v1.GetTotalMass() <= v2.GetTotalMass() ? v2 : v1;
    }
    // Check 3 - If weight is similar, dominant vessel will be the one with the higher ID
    return v1.id.CompareTo(v2.id) <= 0 ? v2 : v1;
  }

				dock.

/*
    if (plugMode == PlugState.PlugDocked) {
      KAS_Shared.DebugLog("PlugHead(Winch) - Plug using docked mode");
      // This should be safe even if already connected
      AttachDocked(portModule);
      // Set attached part
      portModule.part.FindAttachNode(portModule.attachNode).attachedPart = this.part;
      this.part.FindAttachNode(connectedPortNodeName).attachedPart = portModule.part;
      // Remove joints between connector and winch
      KAS_Shared.RemoveAttachJointBetween(this.part, portModule.part);
      headState = PlugState.PlugDocked;
      if (fireSound) {
        AudioSource.PlayClipAtPoint(
            GameDatabase.Instance.GetAudioClip(portModule.plugDockedSndPath),
            portModule.part.transform.position);
      }
      // Kerbal Joint Reinforcement compatibility
      GameEvents.onPartUndock.Fire(portModule.part);
    } * */
			}
		}

		[KSPEvent(name = "ContextMenuDockType", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "docktype")]
		public void ContextMenuDockType()
		{
			Events["ContextMenuDockType"].guiName = "docktype: " + dock.state;

			if(dock.state == "PreAttached")
			{
				try
				{
					Events["ContextMenuDecoupleAndDock"].guiActive = true;
					Events["ContextMenuDecoupleAndDock"].guiActiveUnfocused = true;
					Events["ContextMenuDecoupleAndDock"].active = true;
				}
				catch(Exception)
				{
					//	Debug.Log(String.Format("[ModuleWeldablePart] Error {0} in OnStart", ex.Message));
				}
			}
		}

		public static ModuleDockingNode dock3 = null;

		[KSPEvent(name = "ContextMenuDockSource", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "-- docksource")]
		public void ContextMenuDockSource()
		{
			dock3 = dock;
		}

		[KSPEvent(name = "ContextMenuRedock", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "-- make main dock")]
		public void ContextMenuRedock()
		{
			// undock... gut, das machen wir ganz normal eigentlich
			Part parent = dock.part.parent;
			Vessel vessel = dock.vessel;
			uint referenceTransformId = dock.vessel.referenceTransformId;
			if (parent != dock.otherNode.part)
			{
			//	dock.otherNode.Undock(); -> eigentlich alles verdreht machen... aber, davon gehe ich mal nicht aus, dass dies nötig sein wird jetzt gerade im Test
				return;
			}
			dock.part.Undock(dock.vesselInfo);
		//	dock.part.AddForce(dock.nodeTransform.forward * (-dock.undockEjectionForce * 0.5f));
		//	parent.AddForce(dock.nodeTransform.forward * (dock.undockEjectionForce * 0.5f));

			if (vessel == FlightGlobals.ActiveVessel && vessel[referenceTransformId] == null)
			{
				dock.StartCoroutine(dock.WaitAndSwitchFocus());
			}

			dock.fsm.RunEvent(dock.on_undock);
			dock.otherNode.OnOtherNodeUndock();

			// jetzt das undock same vessel machen aber gleich reaktivieren den Scheiss
				// dabei kommt's nicht drauf an, wer der other node und wer der eigentliche node ist... völlig wurscht

			// geht, aber ich brauch keine Force
			//	dock.part.AddForce(dock.nodeTransform.forward * (-dock.undockEjectionForce * 0.5f));
			//	attachedPart.AddForce(dock.nodeTransform.forward * (dock.undockEjectionForce * 0.5f));

			KFSMCallback e0 = dock3.on_undock.OnEvent;
			KFSMCallback e1 = null;

			// wir schreiben die Funktionen um... -> damit müssten die Ports sofort wieder ready sein...
			dock3.on_undock.OnEvent = delegate
			{
				// if's lass ich weg, ist mir egal
				dock3.on_undock.GoToStateOnEvent = dock3.st_ready; // direkt wieder ready
			};

			if(dock3.otherNode)
			{
				e1 = dock3.otherNode.on_undock.OnEvent;

				// das da oben ist Standard, ich definier das jetzt mal um....
				dock3.otherNode.on_undock.OnEvent = delegate
				{
					// if's lass ich weg, ist mir egal
					dock3.otherNode.on_undock.GoToStateOnEvent = dock3.otherNode.st_ready; // direkt wieder ready
				};
			}

			dock3.fsm.RunEvent(dock3.on_undock);

			dock3.on_undock.OnEvent = e0;

			if(dock3.otherNode)
			{
				dock3.otherNode.OnOtherNodeUndock();

				dock3.otherNode.on_undock.OnEvent = e1;
			}

			return;







			try
			{
				if((dock.state != "Docked (same vessel)") && (dock.otherNode.state != "Docked (same vessel)"))
					return;

				ModuleDockingNode dock2 = dock.otherNode;

				if((dock3.state == "Docked (docker)") || (dock3.state == "Docked (dockee)"))
				{
					ModuleDockingNode dock4 = dock3.otherNode;

					if((dock4.state != "Docked (docker)") && (dock4.state != "Docked (dockee)"))
						return;

					dock.UndockSameVessel(); // hüm?

					dock.fsm.RunEvent(dock.on_nodeDistance);
					dock2.fsm.RunEvent(dock2.on_nodeDistance);

					dock3.Undock();

					dock3.fsm.RunEvent(dock3.on_nodeDistance);
					dock4.fsm.RunEvent(dock4.on_nodeDistance);

					dock.DockToVessel(dock2);

					dock3.DockToSameVessel(dock4);
				}
			}
			catch(Exception)
			{ }
		}

		[KSPEvent(name = "ContextMenuDebugBreak", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "debug break")]
		public void ContextMenuDebugBreak()
		{
			// können wir irgendwie die Position gegenüber dem Docking-Port rauskriegen? -> also, vom nächsten, selbst wenn wir deaktiviert sind??????

//ddd






			dock = dock;

			d1 = dock;
			d2 = dock.otherNode;

			if(dock.part.parent == dock.otherNode.part)
			{
				d1 = dock;
				d2 = dock.otherNode;
			}
			else
			{
				d2 = dock;
				d1 = dock.otherNode;
			}
		}


		[KSPEvent(name = "ContextMenuVermienen", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "-- deaktivieren")]
		public void ContextMenuVermienen()
		{
			on_preattachDecouple = dock.on_preattachedDecouple.OnEvent;
			dock.on_preattachedDecouple.OnEvent = new KFSMCallback(preattachDecoupleCallback);

			on_capture = dock.on_capture.OnEvent;
			dock.on_capture.OnEvent = new KFSMCallback(captureCallback);

			on_capture_dockee = dock.on_capture_dockee.OnEvent;
			dock.on_capture_dockee.OnEvent = new KFSMCallback(capture_dockeeCallback);

			on_capture_docker = dock.on_capture_docker.OnEvent;
			dock.on_capture_docker.OnEvent = new KFSMCallback(capture_dockerCallback);

			on_capture_docker_sameVessel = dock.on_capture_docker_sameVessel.OnEvent;
			dock.on_capture_docker_sameVessel.OnEvent = new KFSMCallback(capture_docker_sameVesselCallback);

		//	dock.on_decouple.OnEvent = new KFSMCallback(decoupleCallback); -> :-) ja richtig, gibt's nicht hier bei der Klasse

			on_disable = dock.on_disable.OnEvent;
			dock.on_disable.OnEvent = new KFSMCallback(disableCallback);

			on_enable = dock.on_enable.OnEvent;
			dock.on_enable.OnEvent = new KFSMCallback(enableCallback);

			on_nodeApproach = dock.on_nodeApproach.OnEvent;
			dock.on_nodeApproach.OnEvent = new KFSMCallback(nodeApproachCallback);

			on_nodeDistance = dock.on_nodeDistance.OnEvent;
			dock.on_nodeDistance.OnEvent = new KFSMCallback(nodeDistanceCallback);

			on_sameVessel_disconnect = dock.on_sameVessel_disconnect.OnEvent;
			dock.on_sameVessel_disconnect.OnEvent = new KFSMCallback(sameVessel_disconnectCallback);

			on_undock = dock.on_undock.OnEvent;
			dock.on_undock.OnEvent = new KFSMCallback(undockCallback);
		}

		[KSPEvent(name = "ContextMenudisable2", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "-- deaktivieren 2")]
		public void ContextMenudisable2()
		{
			if(on_disable2 == null)
			{
				on_disable2 = new KFSMEvent("Disable_man");
				on_disable2.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
				//			on_disable2.OnCheckCondition = ((KFSMState st) => true);
				on_disable2.GoToStateOnEvent = dock.st_disabled;
				dock.fsm.AddEvent(on_disable2, new KFSMState[]
		{
			dock.st_ready,
			dock.st_disengage
		});
			}

			dock.fsm.RunEvent(on_disable2);
		}

		[KSPEvent(name = "ContextMenuenable2", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "-- aktivieren 2")]
		public void ContextMenuenable2()
		{
			if(on_enable2 == null)
			{
				on_enable2 = new KFSMEvent("Enable_man");
				on_enable2.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
				//			on_enable2.OnCheckCondition = ((KFSMState st) => true);
				on_enable2.GoToStateOnEvent = dock.st_ready;
				dock.fsm.AddEvent(on_enable2, new KFSMState[]
		{
			dock.st_disabled
		});
			}

			dock.fsm.RunEvent(on_enable2);
		}
	}
}

