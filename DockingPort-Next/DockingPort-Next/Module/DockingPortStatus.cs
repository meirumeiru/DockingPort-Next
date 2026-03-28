using System;

using UnityEngine;

namespace DockingPortNext
{
	public class DockingPortStatus : IConfigNode
	{
		public float extendPosition;

		public Vector3 activeJointTargetPosition;
		public Quaternion activeJointTargetRotation;

		public float _pushStep;

		public void Load(ConfigNode node)
		{
			node.TryGetValue("extendPosition", ref extendPosition);

			node.TryGetValue("activeJointTargetPosition", ref activeJointTargetPosition);
			node.TryGetValue("activeJointTargetRotation", ref activeJointTargetRotation);

			node.TryGetValue("_pushStep", ref _pushStep);
		}

		public void Save(ConfigNode node)
		{
			node.AddValue("extendPosition", extendPosition);

			if(activeJointTargetPosition != null)	node.AddValue("activeJointTargetPosition", activeJointTargetPosition);
			if(activeJointTargetRotation != null)	node.AddValue("activeJointTargetRotation", activeJointTargetRotation);

			node.AddValue("_pushStep", _pushStep);
		}
	}
}
