using System;

namespace Shared
{
    [Serializable]
    public class PawnJobData
    {
        public string _jobDefName;

        public int _jobThingCount;

        public string _pawnId;

        public bool _isDrafted;

        public TransformDetails _transformComponent = new TransformDetails();

        public PawnTargetDetails _targetComponent = new PawnTargetDetails();
    }
}