using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Services.Interface
{
    /// <summary>
    /// Interface for handling form data related operations
    /// </summary>
    public interface IInstance
    {
        /// <summary>
        /// Gets the instance
        /// </summary>
        Task<Instance> GetInstance(string app, string org, int instanceOwnerPartyId, Guid instanceId);

        /// <summary>
        /// Gets the instance list of a given instance owner.
        /// </summary>
        Task<List<Instance>> GetInstances(int instanceOwnerPartyId);

        /// <summary>
        /// update instance metadata
        /// </summary>
        Task<Instance> UpdateInstance(Instance instance);

        /// <summary>
        /// Creates an instance of an application with no data.
        /// </summary>
        /// <param name="org">Unique identifier of the organisation responsible for the app.</param>
        /// <param name="app">Application identifier which is unique within an organisation.</param>
        /// <param name="instanceTemplate">the instance template to create (must have instanceOwner with partyId, personNumber or organisationNumber set)</param>
        /// <returns>The created instance</returns>
        Task<Instance> CreateInstance(string org, string app, Instance instanceTemplate);
    }
}
