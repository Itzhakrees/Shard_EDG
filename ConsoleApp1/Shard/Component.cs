using System;

namespace Shard
{
    abstract class Component
    {
        private GameObject owner;

        public GameObject Owner { get => owner; set => owner = value; }

        public virtual void initialize()
        {
        }

        public virtual void update()
        {
        }

        public virtual void physicsUpdate()
        {
        }
    }
}
