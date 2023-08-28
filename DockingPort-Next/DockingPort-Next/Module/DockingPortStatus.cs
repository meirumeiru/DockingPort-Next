using System;

using UnityEngine;

namespace DockingPortNext
{
	public class DockingPortStatus : IConfigNode
	{
		public Vector3 ringPosition;
		public Quaternion ringRotation;

		// > st_ready / < st_push
		public float extendPosition;

		// st_push / st_restore
		public Vector3 activeJointTargetPosition;
		public Quaternion activeJointTargetRotation;

		public float _pushStep;

		// > st_captured
		public Vector3 originalRingObjectLocalPosition;
		public Quaternion originalRingObjectLocalRotation;

		// onrails
		public bool followOtherPort = false;

		public Vector3 otherPortRelativePosition;
		public Quaternion otherPortRelativeRotation;

		public void Load(ConfigNode node)
		{
			node.TryGetValue("ringPosition", ref ringPosition);
			node.TryGetValue("ringRotation", ref ringRotation);

			node.TryGetValue("extendPosition", ref extendPosition);

			node.TryGetValue("activeJointTargetPosition", ref activeJointTargetPosition);
			node.TryGetValue("activeJointTargetRotation", ref activeJointTargetRotation);

			node.TryGetValue("_pushStep", ref _pushStep);

			node.TryGetValue("originalRingObjectLocalPosition", ref originalRingObjectLocalPosition);
			node.TryGetValue("originalRingObjectLocalRotation", ref originalRingObjectLocalRotation);

			node.TryGetValue("followOtherPort", ref followOtherPort);

			node.TryGetValue("otherPortRelativePosition", ref otherPortRelativePosition);
			node.TryGetValue("otherPortRelativeRotation", ref otherPortRelativeRotation);
		}

		public void Save(ConfigNode node)
		{
			if(ringPosition != null) node.AddValue("ringPosition", ringPosition);
			if(ringRotation != null) node.AddValue("ringRotation", ringRotation);

			node.AddValue("extendPosition", extendPosition);

			if(activeJointTargetPosition != null)	node.AddValue("activeJointTargetPosition", activeJointTargetPosition);
			if(activeJointTargetRotation != null)	node.AddValue("activeJointTargetRotation", activeJointTargetRotation);

			node.AddValue("_pushStep", _pushStep);

			if(originalRingObjectLocalPosition != null)	node.AddValue("originalRingObjectLocalPosition", originalRingObjectLocalPosition);
			if(originalRingObjectLocalRotation != null)	node.AddValue("originalRingObjectLocalRotation", originalRingObjectLocalRotation);

			node.AddValue("followOtherPort", followOtherPort);

			if(otherPortRelativePosition != null)	node.AddValue("otherPortRelativePosition", otherPortRelativePosition);
			if(otherPortRelativeRotation != null)	node.AddValue("otherPortRelativeRotation", otherPortRelativeRotation);
		}
	}
}
