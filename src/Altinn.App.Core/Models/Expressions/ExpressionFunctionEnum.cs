namespace Altinn.App.Core.Models.Expressions;

/// <summary>
/// Enumeration for valid functions in Layout Expressions
/// </summary>
public enum ExpressionFunction
{
    /// <summary>
    /// Value for all unknown functions.
    /// </summary>
    INVALID,
    /// <summary>
    /// Lookup in datamodel (respect current context for missing indexes for repeating groups)
    /// </summary>
    dataModel,
    /// <summary>
    /// Lookup data in simpleBinding for a component with this ID
    /// </summary>
    component,
    /// <summary>
    /// Lookup a few properties from the instance
    /// </summary>
    instanceContext,
    /// <summary>
    /// Conditional
    /// </summary>
    @if,
    /// <summary>
    /// Lookup settings from the `frontendSettings` key in appsettings.json (or any environment overrides)
    /// </summary>
    frontendSettings,
    /// <summary>
    /// Concat strings
    /// </summary>
    concat,
    /// <summary>
    /// Check if values are equal
    /// </summary>
    equals,
    /// <summary>
    /// <see cref="equals" />
    /// </summary>
    notEquals,
    /// <summary>
    /// Compare numerically
    /// </summary>
    greaterThanEq,
    /// <summary>
    /// Compare numerically
    /// </summary>
    lessThan,
    /// <summary>
    /// Compare numerically
    /// </summary>
    lessThanEq,
    /// <summary>
    /// Compare numerically
    /// </summary>
    greaterThan,
    /// <summary>
    /// Return true if all the expressions evaluate to true
    /// </summary>
    and,
    /// <summary>
    /// Return true if any of the expressions evaluate to true
    /// </summary>
    or,
    /// <summary>
    /// Return true if the single argument evaluate to false, otherwise return false
    /// </summary>
    not,
    /// <summary>
    /// Get the action performed in task prior to bpmn gateway
    /// </summary>
    gatewayAction,
}