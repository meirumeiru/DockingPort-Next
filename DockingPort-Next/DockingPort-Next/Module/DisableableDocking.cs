using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KAS
{
	public class KASDisableableDocking : KASModuleHarpoon
	{
		[KSPField(isPersistant = false)]
		public bool activated = true;

		private ModuleDockingNode dock = null;

		public void Start()
		{
//			if(!HighLogic.LoadedSceneIsFlight)
//				return;

			try
			{
				dock = part.FindModuleImplementing<ModuleDockingNode>();
			}
			catch(Exception)
			{}
		}

		// FEHLER, meine Idee mal
		[KSPEvent(name = "ContextMenuActivateDock", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "deactivate docking")]
		public void ContextMenuActivateDock()
		{
			activated = !activated;
			Events["ContextMenuActivateDock"].guiName = activated ? "deactivate docking" : "activate docking";

			if(dock != null)
			{
				if(activated)
					dock.fsm.RunEvent(dock.on_enable);
				else
					dock.fsm.RunEvent(dock.on_disable);
				dock.fsm.FixedUpdateFSM();
			}
		}

/*
	Button bauen um den "Dock auf das hier umzuhängen" ... also -> zweiten Dock suchen und prüfen,
 * ob dieser der wirklich aktive Hauptknoten ist... von meinem Dingens da...
 * wenn er's ist -> diesen abhängen und meinen anhängen... ... erstmal versuchen ohne
 * grabber... von second-dock nach primär-dock wechseln versuchen...
 */
		private bool bb = true;

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

		[KSPEvent(name = "ContextMenuMakePrimaryDock", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "make primary")]
		public void ContextMenuMakePrimaryDock()
		{
			dock = dock;

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

			ModuleDockingNode dn;

			foreach(Vessel vv in FlightGlobals.VesselsLoaded)
			{
				if(vv.rootPart != null)
				{
					foreach(ModuleDockingNode dockingNode in vv.rootPart.FindModulesImplementing<ModuleDockingNode>())
					{
						dn = dockingNode;
					}
				}
			}

			bool undock = true;

			if(undock)
				dock.Decouple(); // -> geht offenbar... mal sehen was das andere tut

			foreach(Vessel vv in FlightGlobals.VesselsLoaded)
			{
				if(vv.rootPart != null)
				{
					foreach(ModuleDockingNode dockingNode in vv.rootPart.FindModulesImplementing<ModuleDockingNode>())
					{
						dn = dockingNode;
					}
				}
			}

			dock.FixedUpdate();
			dock.Update();

			return;

			{
				//		dock.Decouple(); -> geht offenbar... mal sehen was das andere tut

				ModuleDockingNode target = dock.otherNode;

				dock.fsm.RunEvent(dock.on_preattachedDecouple);
				target.fsm.RunEvent(target.on_preattachedDecouple);

				dock.fsm.UpdateFSM();
				target.fsm.UpdateFSM();
			}

			return;

			{

				if(bb)
				{
					// dock.Decouple();
					dock.state = "Disengage";
					dock.otherNode.state = "Disengage";
				}

				return;

				Vessel v = dock.GetVessel();
				ModuleDockingNode target = dock.otherNode;

				int targetid = target.part.GetInstanceID();

				Part p = dock.part.parent;

				bool b = true, c = true, d = true, e = true, f = true;

				if(b)
				{
					dock.Decouple();

					if(c)
					{
						dock.fsm.RunEvent(dock.on_preattachedDecouple);
						target.fsm.RunEvent(target.on_preattachedDecouple);

						//			target.fsm.RunEvent(target.on_undock); // ?? weiss nicht ob's hilft
						//			target.fsm.UpdateFSM();

						//			dock.fsm.UpdateFSM();
						//			target.fsm.UpdateFSM();
					}

					/*				target.vessel.
					// mal sehen :-)*/
					ModuleDockingNode target2 = null;


					target.vessel.UpdateCaches();
					target.vessel.UpdateVesselModuleActivation();
					target.vessel.UpdateLandedSplashed();


					foreach(Vessel vv in FlightGlobals.VesselsLoaded)
					{
						if(vv.rootPart != null)
						{
							foreach(ModuleDockingNode dockingNode in vv.rootPart.FindModulesImplementing<ModuleDockingNode>())
							{
								if(dockingNode.part.GetInstanceID() == targetid)
								{
									target2 = dockingNode;
								}
							}
						}
					}

					bool q = false;
					if(q)
						target = target2;
				}

				if(d)
				{
					dock.fsm.RunEvent(dock.on_nodeDistance);
					target.fsm.RunEvent(target.on_nodeDistance);

					//			dock.fsm.RunEvent(dock.on_nodeApproach);
					dock.fsm.RunEvent(dock.on_capture_dockee);

					//			target.fsm.RunEvent(target.on_nodeApproach);
					target.fsm.RunEvent(target.on_capture_docker);
				}

				if(e)
				{
					dock.DockToVessel(target);

					if(f)
					{
						dock.fsm.RunEvent(dock.on_capture_dockee);
						target.fsm.RunEvent(target.on_capture_docker);

						target.state = "Docked (docker)";
					}

					//		dock.fsm.FixedUpdateFSM();
					//		target.fsm.FixedUpdateFSM();
				}
			}
		}

		[KSPEvent(name = "ContextMenuMakePrimaryDock2", active = true, guiActive = true, guiActiveUnfocused = false, guiName = "make primary2")]
		public void ContextMenuMakePrimaryDock2()
		{
			// undock

			ModuleDockingNode dock2 = dock.otherNode;

	//		dock.minDistanceToReEngage = 0;
	//		dock2.minDistanceToReEngage = 0;

			dock.Decouple();
				// ich sage, das löst on_undock aus auf beiden Teils (ja, tut es, weiss ich ja)
			
			// die Frage ist nur, ob man's mit diesem fsm.RunEvent macht oder wie oder was :-)

			// entfernen

			dock.fsm.RunEvent(dock.on_nodeDistance);
			dock2.fsm.RunEvent(dock2.on_nodeDistance);

			// annähern

	//		dock.fsm.RunEvent(dock.on_nodeApproach);
	//		dock2.fsm.RunEvent(dock2.on_nodeApproach);

	//		dock.fsm.RunEvent(dock.on_capture_dockee);
	//		dock2.fsm.RunEvent(dock2.on_capture_docker);

			// dock?

			dock.DockToSameVessel(dock2);
	//		dock.DockToVessel(dock2);	// es ist super komisch was passiert... mal sehen ob ich's umdrehen kann*/


//			jetzt probieren das endlich mal durchzuziehen, verdammt noch eins

		}
	}
}

