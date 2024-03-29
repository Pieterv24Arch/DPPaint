﻿using DPPaint.Shapes;
using DPPaint.Strategy;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using DPPaint.Decorators;

namespace DPPaint.Commands.UserAction
{
    /// <summary>
    /// This command handles the opening of save files
    /// </summary>
    public class OpenFileCommand : IUserActionCommand
    {
        #region Properties

        /// <inheritdoc />
        public List<PaintBase> ShapeList { get; set; }
        /// <inheritdoc />
        public Stack<List<PaintBase>> UndoStack { get; set; }
        /// <inheritdoc />
        public Stack<List<PaintBase>> RedoStack { get; set; }

        #endregion

        private readonly ICanvasPage _page;

        public OpenFileCommand(ICanvasPage page)
        {
            _page = page;
        }


        #region Command pattern entries

        /// <inheritdoc />
        public void ExecuteUserAction()
        {
            ExecuteUserActionAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public async Task ExecuteUserActionAsync()
        {
            var openPicker = new FileOpenPicker();

            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".json");
            openPicker.ViewMode = PickerViewMode.List;

            StorageFile file = await openPicker.PickSingleFileAsync();

            if (file != null)
            {
                string jsonString = await FileIO.ReadTextAsync(file);

                List<PaintBase> newShapeList = DeserializeJsonSave(jsonString);

                // Clear undo, redo and master list
                UndoStack.Clear();
                RedoStack.Clear();
                ShapeList.Clear();
                // Add deserialized master list to main page
                ShapeList.AddRange(newShapeList);

                // Send draw and update list commands to main page
                _page.Draw();
                _page.UpdateList();
            }
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Convert json string into a state with objects
        /// </summary>
        /// <param name="jsonSaveString">savefile as json string</param>
        /// <returns>Canvas state</returns>
        private List<PaintBase> DeserializeJsonSave(string jsonSaveString)
        {
            List<PaintBase> newShapeList = new List<PaintBase>();

            // Savefile should contain json array at root
            JArray jArray = JArray.Parse(jsonSaveString);
            if (jArray != null)
            {
                // Iterate through items in array
                foreach (JToken jToken in jArray)
                {
                    if (jToken is JObject jObject)
                    {
                        if (Enum.TryParse(jObject.GetValue("type").ToString(), out PaintType type))
                        {
                            // Get base properties
                            PaintBaseProperties deserializedProperties = GetBaseProperties(jObject);

                            if (deserializedProperties != null)
                            {
                                if (type == PaintType.Shape)
                                {
                                    PaintShape shape = GetPaintShape(jObject, deserializedProperties);
                                    if (shape != null)
                                    {
                                        newShapeList.Add(AddDecorators(jObject, shape));
                                    }
                                }
                                else if (type == PaintType.Group)
                                {
                                    PaintGroup group = GetPaintGroup(jObject, deserializedProperties);
                                    if (group != null)
                                    {
                                        newShapeList.Add(AddDecorators(jObject, group));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return newShapeList;
        }

        /// <summary>
        /// Deserialize base properties from JObject
        /// </summary>
        /// <param name="jObject">json object</param>
        /// <returns>Base shape properties</returns>
        private PaintBaseProperties GetBaseProperties(JObject jObject)
        {
            // Check if all values were found
            bool overallCompletion = true;

            double width = 0;
            double height = 0;
            double x = 0;
            double y = 0;

            overallCompletion = overallCompletion && double.TryParse(jObject.GetValue("width").ToString(), out width);
            overallCompletion = overallCompletion && double.TryParse(jObject.GetValue("height").ToString(), out height);
            overallCompletion = overallCompletion && double.TryParse(jObject.GetValue("x").ToString(), out x);
            overallCompletion = overallCompletion && double.TryParse(jObject.GetValue("y").ToString(), out y);

            if (overallCompletion)
            {
                return new PaintBaseProperties
                {
                    Width = width,
                    Height = height,
                    X = x,
                    Y = y
                };
            }

            return null;
        }

        /// <summary>
        /// Convert json object to PaintShape
        /// </summary>
        /// <param name="jObject">json object</param>
        /// <param name="baseProps">base properties</param>
        /// <returns>initialized paintshape</returns>
        private PaintShape GetPaintShape(JObject jObject, PaintBaseProperties baseProps)
        {
            if (jObject.GetValue("shapeType").ToString() == CircleShape.Instance.ToString() || jObject.GetValue("shapeType").ToString() == RectangleShape.Instance.ToString())
            {
                // Determine shape
                IShapeBase shape = null;
                if (jObject.GetValue("shapeType").ToString() == CircleShape.Instance.ToString()) shape = CircleShape.Instance;
                if (jObject.GetValue("shapeType").ToString() == RectangleShape.Instance.ToString()) shape = RectangleShape.Instance;

                // Create and return paintshape
                return new PaintShape(shape)
                {
                    Height = baseProps.Height,
                    Width = baseProps.Width,
                    X = baseProps.X,
                    Y = baseProps.Y
                };
            }

            return null;
        }

        /// <summary>
        /// Deserialize paint group
        /// </summary>
        /// <param name="jObject">paintgroup JObject</param>
        /// <param name="baseProps">basic shape properties</param>
        /// <returns>Paintgroup</returns>
        private PaintGroup GetPaintGroup(JObject jObject, PaintBaseProperties baseProps)
        {
            PaintGroup group = new PaintGroup
            {
                Height = baseProps.Height,
                Width = baseProps.Width,
                X = baseProps.X,
                Y = baseProps.Y
            };

            if (jObject.GetValue("children") is JArray children)
            {
                // Deserialize children
                foreach (JToken child in children)
                {
                    if (child is JObject jChild &&
                        Enum.TryParse(jChild.GetValue("type").ToString(), out PaintType type))
                    {
                        PaintBaseProperties deserBase = GetBaseProperties(jChild);

                        if (deserBase != null)
                        {
                            if (type == PaintType.Shape)
                            {
                                PaintShape shape = GetPaintShape(jChild, deserBase);
                                if (shape != null)
                                {
                                    group.Add(AddDecorators(jChild, shape));
                                }
                            }
                            else if (type == PaintType.Group)
                            {
                                // If child is a group, recursively deserialize to object
                                PaintGroup innerGroup = GetPaintGroup(jChild, deserBase);
                                if (innerGroup != null)
                                {
                                    group.Add(AddDecorators(jChild, innerGroup));
                                }
                            }
                        }
                    }
                }
            }

            return group;
        }

        /// <summary>
        /// Add decorators to the instantiated objects
        /// </summary>
        /// <param name="jObject">decorator jobject</param>
        /// <param name="paintBase">paint element to decorate</param>
        /// <returns>Decorated paintbase of paintbase when there are no decorated in jobject</returns>
        private PaintBase AddDecorators(JObject jObject, PaintBase paintBase)
        {
            if (jObject.GetValue("decorators") is JArray decorators)
            {
                foreach (JToken decorator in decorators)
                {
                    // Check for valid decorator
                    if (decorator is JObject jDecorator &&
                        (jDecorator.ContainsKey("position") && jDecorator.ContainsKey("decoration")))
                    {
                        // Decorate paintBase
                        switch (jDecorator.GetValue("position").ToString())
                        {
                            case "Top":
                                paintBase = new TopDecoration(paintBase, jDecorator.GetValue("decoration").ToString());
                                break;
                            case "Bottom":
                                paintBase = new BottomDecoration(paintBase, jDecorator.GetValue("decoration").ToString());
                                break;
                            case "Left":
                                paintBase = new LeftDecoration(paintBase, jDecorator.GetValue("decoration").ToString());
                                break;
                            case "Right":
                                paintBase = new RightDecoration(paintBase, jDecorator.GetValue("decoration").ToString());
                                break;
                        }
                    }
                }
            }

            return paintBase;
        }
    }

    #endregion
}
