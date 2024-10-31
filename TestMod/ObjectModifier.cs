using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace FromJianghuENMod
{
    /// <summary>
    /// Factory class for creating instances of ObjectModifier subclasses based on a category string.
    /// </summary>
    public static class ObjectModifierFactory
    {
        /// <summary>
        /// Creates an instance of an ObjectModifier subclass based on the provided category and input string.
        /// </summary>
        /// <param name="category">The category string used to determine the subclass type. Must match the subclass type</param>
        /// <param name="fromString">The input string used to initialize the ObjectModifier instance.</param>
        /// <param name="instance">The created ObjectModifier instance if successful, otherwise null.</param>
        /// <returns>True if the ObjectModifier instance was created and parsed successfully, otherwise false.</returns>
        public static bool CreateObjectModifier(string category, string fromString, out ObjectModifier instance)
        {
            // Remove all special characters from the category string
            category = Regex.Replace(category, @"[^\w]", "");
            Type objectModifierSubClass = Type.GetType($"FromJianghuENMod.{category}");
            if (objectModifierSubClass == null)
            {
                FJDebug.Log($"Failed to create an ObjectModifier object from the input string: {fromString}. category: {category}");
                instance = null;
                return false;
            }

            try
            {
                instance = (ObjectModifier)Activator.CreateInstance(objectModifierSubClass, fromString);
                // Return instance.TryParse
                return instance.TryParse(out instance);
            }
            catch (Exception ex)
            {
                FJDebug.LogError($"Exception occurred while creating ObjectModifier: {ex.Message}");
                instance = null;
                return false;
            }
        }
    }

    /// <summary>
    /// Abstract base class for modifying Unity components based on a parsed input string.
    /// </summary>
    public abstract class ObjectModifier
    {
        protected string RawString { get; set; }
        public abstract string RegexString { get; }
        public string FullPath { get; set; }

        /// <summary>
        /// Checks if the given component matches the target object based on its full path.
        /// </summary>
        /// <param name="obj">The component to check.</param>
        /// <returns>True if the component matches the target object, otherwise false.</returns>
        public bool IsTargetObject(Component obj) => Helpers.GetFullPathToObject(obj) == FullPath;

        public abstract bool CanBeApplied(Component obj);
        public abstract void Apply(Component obj);

        /// <summary>
        /// Initializes a new instance of the ObjectModifier class with the specified input string.
        /// </summary>
        /// <param name="fromString">The input string used to initialize the ObjectModifier instance.</param>
        public ObjectModifier(string fromString) => RawString = fromString;
        //write summary
        /// <summary>
        /// Tries to parse the input string into the specified ObjectModifier subclass instance.
        /// </summary>
        /// <typeparam name="T">The type of the ObjectModifier subclass.</typeparam>
        /// <param name="modifierSubClassInstance">The parsed ObjectModifier subclass instance.</param>
        /// <returns>True if parsing was successful, otherwise false.</returns>
        public abstract bool TryParse<T>(out T modifierSubClassInstance) where T : ObjectModifier;
    }

    /// <summary>
    /// Class for resizing Unity components based on a parsed input string.
    /// </summary>
    public class ObjectResizer : ObjectModifier
    {
        private Vector2 SizeChange { get; set; }
        public override string RegexString => @"(?<fullPath>[\w\/.()]+)\s\=\s(?<sizeChange>.+)";

        /// <summary>
        /// Initializes a new instance of the ObjectResizer class with the specified input string.
        /// </summary>
        /// <param name="fromString">The input string used to initialize the ObjectResizer instance.</param>
        public ObjectResizer(string fromString) : base(fromString) { }

        public override bool CanBeApplied(Component obj) => IsTargetObject(obj);

        public override string ToString()
        {
            return $"{FullPath} = {SizeChange.x};{SizeChange.y}";
        }

        public override bool TryParse<T>(out T modifierSubClassInstance)
        {
            modifierSubClassInstance = default;

            // Match the input string with the expected format
            Match match = Regex.Match(RawString, RegexString);
            if (match.Success)
            {
                FullPath = match.Groups["fullPath"].Value;
                string sizeChange = match.Groups["sizeChange"].Value;

                // Parse the size change value
                string[] sizeParts = sizeChange.Split(';');
                if (sizeParts.Length == 2 &&
                    float.TryParse(sizeParts[0], out float width) &&
                    float.TryParse(sizeParts[1], out float height))
                {
                    SizeChange = new Vector2(width, height);
                    modifierSubClassInstance = this as T;
                }
            }

            if (modifierSubClassInstance == null)
                FJDebug.LogError($"Failed to create an ObjectResizer object from the input string: {RawString}");
            else
                FJDebug.Log($"Created {GetType()} successfully!");

            return modifierSubClassInstance != null;
        }

        public override void Apply(Component obj)
        {
            FJDebug.Log($"Applying size change to {obj.name}");
            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = SizeChange;
            FJDebug.Log($"Changing size to {rectTransform.sizeDelta}");
        }
    }
    /// <summary>
    /// Class for changing layout group properties of Unity components based on a parsed input string.
    /// </summary>
    public class LayoutGroupChanger : ObjectModifier
    {
        private Vector2? CellSizeChange { get; set; }
        private Vector2? SpacingChange { get; set; }
        public override string RegexString => @"(?<fullPath>[\w\/.()]+)\s(?<property>\w+)\s=\s(?<value>.+)";

        /// <summary>
        /// Initializes a new instance of the LayoutGroupChanger class with the specified input string.
        /// </summary>
        /// <param name="fromString">The input string used to initialize the LayoutGroupChanger instance.</param>
        public LayoutGroupChanger(string fromString) : base(fromString) { }

        /// <summary>
        /// Checks if the layout group can be applied based on the parsed parameters.
        /// </summary>
        /// <param name="group">The layout group component to check.</param>
        /// <returns>True if the layout group can be applied, otherwise false.</returns>
        public bool CanBeAppliedByParameters(Component group)
        {
            // Cellsize is only applicable to GridLayoutGroup. Everything else is applicable to all LayoutGroups
            if (CellSizeChange != null && group is not GridLayoutGroup gridLayoutGroup)
            {
                return false;
            }
            return group is LayoutGroup;
        }

        public override bool CanBeApplied(Component group) => IsTargetObject(group) && CanBeAppliedByParameters(group);

        public override string ToString()
        {
            return $"Path: {FullPath}, CellSizeChange: {CellSizeChange}, SpacingChange: {SpacingChange}";
        }

        public override void Apply(Component group)
        {
            FJDebug.Log($"Applying layout changer to {group.name}");

            if (CellSizeChange.HasValue && group is GridLayoutGroup gridLayoutGroup)
            {
                gridLayoutGroup.cellSize = CellSizeChange.Value;
                FJDebug.Log($"Changing cellsize to {gridLayoutGroup.cellSize}");
            }

            if (SpacingChange.HasValue)
            {
                if (group is HorizontalOrVerticalLayoutGroup layoutGroup)
                {
                    layoutGroup.spacing = SpacingChange.Value.x;
                    FJDebug.Log($"Changing spacing to {layoutGroup.spacing}");
                }
                else if (group is GridLayoutGroup gridLayout)
                {
                    gridLayout.spacing = SpacingChange.Value;
                    FJDebug.Log($"Changing spacing to {gridLayout.spacing}");
                }
            }
        }

        public override bool TryParse<T>(out T modifierSubClassInstance)
        {
            // Match the input string with the expected format
            Match match = Regex.Match(RawString, RegexString);
            if (match.Success)
            {
                string property = match.Groups["property"].Value;
                string value = match.Groups["value"].Value;

                // Parse the value based on the property name
                switch (property.ToLower())
                {
                    case "cellsize":
                        string[] cellSizeParts = value.Split(';');
                        if (cellSizeParts.Length == 2 &&
                            float.TryParse(cellSizeParts[0], out float cellWidth) &&
                            float.TryParse(cellSizeParts[1], out float cellHeight))
                        {
                            CellSizeChange = new Vector2(cellWidth, cellHeight);
                        }
                        break;
                    case "spacing":
                        string[] spacing = value.Split(';');
                        // if spacing is a single value, apply it to both x and y
                        if (spacing.Length == 1 &&
                            float.TryParse(spacing[0], out float spacingValue))
                        {
                            SpacingChange = new(spacingValue, spacingValue);
                        }
                        // if spacing is two values, apply them to x and y respectively
                        else if (spacing.Length == 2 &&
                            float.TryParse(spacing[0], out float spacingX) &&
                            float.TryParse(spacing[1], out float spacingY))
                        {
                            SpacingChange = new(spacingX, spacingY);
                        }
                        break;
                    default:
                        FJDebug.LogError($"Unsupported property name: {property}");
                        modifierSubClassInstance = null;
                        return false;
                }

                // Create the LayoutGroupResizerInfo object
                modifierSubClassInstance = this as T;
                FJDebug.Log($"Created {GetType()} successfully!");
            }
            else
            {
                FJDebug.LogError($"Failed to create a LayoutGroupResizerInfo object from the input string: {RawString}");
                modifierSubClassInstance = null;
            }
            return modifierSubClassInstance != null;
        }
    }
}
