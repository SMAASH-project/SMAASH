using UnityEngine;

namespace Fusion.Addons.Physics
{
  /// <summary>
  /// Options for whether Unity will auto-simulate or Fusion will call Physics.Simulate().
  /// Auto will make Fusion the simulation authority in all cases except Single-Peer Shared Mode.
  /// </summary>
  public enum PhysicsAuthorities {
    /// <summary>
    /// Automatically determine if Unity or Physics should be calling Physics.Simulate.
    ///  Will make Fusion the simulation authority in all cases except Single-Peer Shared Mode.
    /// </summary>
    Auto,
    /// <summary>
    /// Physics will always be auto-simulated by Unity Physics.
    /// </summary>
    Unity,
    /// <summary>
    /// Physics.Simulate() will be called by a <see cref="RunnerSimulatePhysicsBase"/> derived component on the Runner.
    /// </summary>
    Fusion,
  }

  /// <summary>
  /// Timing segment options for when Physics.Simulate() occurs.
  /// These enum values align with Unity's SimulationMode and SimulationMode2D enums, and have FixedUpdateNetwork added.
  /// </summary>
  public enum PhysicsTimings {
    /// <summary>
    /// Calls to Physics.Simulate() are automatically called every Unity FixedUpdate()
    /// </summary>
    FixedUpdate = SimulationMode2D.FixedUpdate,
    /// <summary>
    /// Calls to Physics.Simulate() are automatically called every Update()
    /// </summary>
    Update = SimulationMode2D.Update,
    /// <summary>
    /// Calls to Physics.Simulate() are handled by user code
    /// </summary>
    Script = SimulationMode2D.Script,
    /// <summary>
    /// Calls to Physics.Simulate() are automatically called every Unity FixedUpdateNetwork()
    /// </summary>
    FixedUpdateNetwork,
  }

  /// <summary>
  /// Defines behavior of physics on clients.
  /// </summary>
  public enum ClientPhysicsSimulation {
    /// <summary>
    /// Physics simulation doesn't run. Objects are in remote time and will be smoothly interpolated locally. The cheapest 
    /// option, however on clients these objects are effectively non-physical and cannot be interacted with. Since 
    /// SyncTransforms() is not called for these objects their Rigidbody is not moved within Unity's Physics and cannot 
    /// be e.g. raycast against.
    /// </summary>
    Disabled = 0,
    /// <summary>
    /// Physics.SyncTransform() is called every tick. Objects are in remote time and will be smoothly interpolated locally. 
    /// As Disabled, but SyncTransforms() is called for all ticks, so that their Rigidbody is correctly positioned in 
    /// Unity's Physics. Costs more than Disabled, due to SyncTransforms() and enables e.g. raycasting.
    /// </summary>
    SyncTransforms = 1,
    /// <summary>
    /// Physics.SyncTransform() is called in resimulation ticks, Physics.Simulate() is called in forward ticks. Objects are 
    /// in remote time and will be smoothly interpolated locally. Having the local Unity Physics run on forward ticks 
    /// ensures that they will be extracted from any collsions e.g. with the local predicted players. Costs more than 
    /// SyncTransforms due to running local physics on forward ticks. Local predicted players can interact to a limited 
    /// degree with these objects, but the interaction may not be completely smooth due to the difference between local, 
    /// predicted time and remote time. 
    /// </summary>
    SimulateForward = 2,
    /// <summary>
    /// Physics.Simulate() is called every tick. Physics simulation is run on both resimulation and forward ticks to predict 
    /// objects to local time. This gives the highest quality interaction between physics objects and local players, at the 
    /// highest CPU cost. 
    /// </summary>
    SimulateAlways = 3,
  }
}
