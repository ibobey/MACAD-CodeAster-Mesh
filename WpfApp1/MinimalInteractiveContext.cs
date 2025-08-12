using Macad.Interaction;
using Macad.Core;

namespace CodeAsterMesh
{
    public class MinimalInteractiveContext : InteractiveContext
    {
        #region Constructors

        public MinimalInteractiveContext() : base() // base() constructor sets InteractiveContext.Current = this;
        {
            // Ensure Parameters is initialized and can provide default sets
            // The base InteractiveContext constructor should initialize Parameters.
            // We add specific parameter sets if they are not already there by default.
            if (this.Parameters.Get<ViewportParameterSet>() == null)
            {
                this.Parameters.Add(new ViewportParameterSet());
            }
            if (this.Parameters.Get<ViewportParameterSet> == null)
            {
                this.Parameters.Add(new ViewportParameterSet());
            }
        }

        #endregion Constructors

        #region Methods

        // Override abstract methods with minimal implementation
        public override void SaveLocalSettings(string name, object obj)
        { /* No-op for minimal setup */
        }

        public override T LoadLocalSettings<T>(string name) where T : class
        {
            return null; /* No-op */
        }

        #endregion Methods
    }
}