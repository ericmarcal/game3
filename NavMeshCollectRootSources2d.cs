using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace NavMeshComponents.Extensions
{
    [ExecuteAlways]
    [AddComponentMenu("Navigation/NavMeshCollectRootSources2d", 30)]
    public class NavMeshCollectRootSources2d : NavMeshExtension
    {
        [SerializeField]
        private List<GameObject> _rootSources;

        public List<GameObject> RooySources { get => _rootSources; set => _rootSources = value; }

        // ***** ALTERAÇÃO FEITA AQUI *****
        protected override void Start() // Renomeado de Awake para Start
        {
            Order = -1000;
            base.Start(); // Chamando o método Start da base
        }

        public override void CollectSources(NavMeshSurface surface, List<NavMeshBuildSource> sources, NavMeshBuilderState navNeshState)
        {
            navNeshState.roots = _rootSources;
        }
    }
}