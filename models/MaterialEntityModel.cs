using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using CreatePipe.Utils;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CreatePipe.Models
{
    //public class MaterialEntityModel : ObserverableObject
    public class MaterialEntityModel : INotifyCollectionChanged
    {
        public Material Material { get; set; }
        public MaterialEntityModel(Material material)
        {
            if (material != null)
            {
                Material = material;
                //_category = material.MaterialCategory;
                _class = material.MaterialClass;
                _colorValue = GetColorValue(material.Color);
                colorElemId = material.Id.ToString();
            }
        }
        public string colorElemId;
        public string MaterialId
        {
            get => colorElemId;
            set
            {
                colorElemId = value;
            }
        }
        //public string _category;

        //public string MaterialCategory
        //{
        //    get => _category;
        //    set
        //    {
        //        _category = value;
        //    }
        //}
        public string _class;
        public string MaterialClass
        {
            get => _class;
            set
            {
                //_class = value;
                Document.NewTransaction(() => Material.MaterialClass = value, "修改类型");
                OnPropertyChanged();
            }
        }
        public string _colorValue;

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void SetProperty<T>(ref T store, T v, [CallerMemberName] string propertyName = null)
        {
            store = v;
            this.OnPropertyChanged(propertyName);
        }

        public string ColorValue
        {
            get => _colorValue;
            set
            {
                _colorValue = value;
            }
        }
        public bool IsSelected { get; set; }
        public ElementId Id { get => Material.Id; }
        public Document Document { get => Material.Document; }
        public string Name
        {
            get => Material.Name;
            set
            {
                //Material.Name= value;
                Document.NewTransaction(() => Material.Name = value, "修改名称");
                OnPropertyChanged();
            }
        }
        public Color Color
        {
            get => Material.Color;
            set
            {
                if (Material.Color != value)
                {
                    //Material.Color = value;
                    Document.NewTransaction(() => Material.Color = value, "修改颜色");
                    OnPropertyChanged();
                }
            }
        }
        public Color AppearanceColor
        {
            get => GetAppearanceColor();
            set
            {
                Set(value, (x) => { Document.NewTransaction(() => SetAppearanceColor(x), "修改外观颜色"); });
            }
        }
        protected void Set<T>(T value, Action<T> callback, [CallerMemberName] string name = null)
        {
            callback?.Invoke(value);
            OnPropertyChanged(name);
        }
        private AssetPropertyDoubleArray4d GetColorProperty(Asset asset)
        {
            return (AssetPropertyDoubleArray4d)asset?.FindByName("generic_diffuse");
        }
        private Color GetAppearanceColor()
        {
            ElementId id = Material.AppearanceAssetId;
            if (id != null && id.IntegerValue != -1)
            {
                AppearanceAssetElement appearanceAssetElement = Document.GetElement(id) as AppearanceAssetElement;
                Asset asset = appearanceAssetElement.GetRenderingAsset();
                AssetPropertyDoubleArray4d property = (AssetPropertyDoubleArray4d)asset?.FindByName("generic_diffuse");
                return property?.GetValueAsColor();
            }
            return null;
        }

        private void SetAppearanceColor(Color color)
        {
            ElementId id = Material.AppearanceAssetId;
            if (id != null && id.IntegerValue != -1)
            {
                using (AppearanceAssetEditScope scope = new AppearanceAssetEditScope(Document))
                {
                    Asset asset = scope.Start(id);
                    GetColorProperty(asset)?.SetValueAsColor(color);
                    scope.Commit(true);
                }
            }
        }
        public string GetColorValue(Color color)
        {
            string colorvalue;
            colorvalue = color.Red.ToString() + "-" + color.Green.ToString() + "-" + color.Blue.ToString();
            return colorvalue;
        }
    }
}
