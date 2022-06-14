using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.VisualBasic.CompilerServices;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Models;

namespace Rumble.Platform.Config.Models;

public class EndpointListing : PlatformDataModel
{
	public long LastUpdated { get; set; }
	public Endpoint[] Endpoints { get; set; }
}

public class Endpoint : PlatformDataModel
{
	public string HttpMethod { get; set; }
	public string RelativeUrl { get; set; }
}

public interface IMetadataAttribute
{
	Attribute[] Process();
}

public class Foo : Attribute, IMetadataAttribute
{
	private string _databaseKey { get; init; }
	private string _friendlyKey { get; init; }
	public Foo(string dbDatabaseKey, string friendlyKey)
	{
		_databaseKey = dbDatabaseKey;
		_friendlyKey = friendlyKey;
	}
	
	public Attribute[] Process()
	{
		return new Attribute[]
		{
			new BsonElementAttribute(_databaseKey),
			new JsonPropertyNameAttribute(name: _friendlyKey)
		};
	}
}

public class FooPropDescriptor : PropertyDescriptor
{
	private PropertyDescriptor _original;

	public FooPropDescriptor(PropertyDescriptor original) : base(original) => _original = original;

	public override AttributeCollection Attributes
	{
		get
		{
			var attributes = base.Attributes.Cast<Attribute>();
			var result = new List<Attribute>();
			foreach (var item in attributes)
			{
				if(item is IMetadataAttribute)
				{
					var attrs = ((IMetadataAttribute)item).Process();
					if(attrs !=null )
					{
						foreach (var a in attrs)
							result.Add(a);
					}
				}
				else
					result.Add(item);
			}
			return new AttributeCollection(result.ToArray());
		}
	}
	
	public FooPropDescriptor(MemberDescriptor descr) : base(descr) { }
	public FooPropDescriptor(MemberDescriptor descr, Attribute[]? attrs) : base(descr, attrs) { }
	
	public FooPropDescriptor(string name, Attribute[]? attrs) : base(name, attrs) { }

	public override bool CanResetValue(object component) => _original.CanResetValue(component);
	public override object? GetValue(object? component) => _original.GetValue(component);
	public override void ResetValue(object component) => _original.ResetValue(component);
	public override void SetValue(object? component, object? value) => _original.SetValue(component, value);
	public override bool ShouldSerializeValue(object component) => _original.ShouldSerializeValue(component);

	public override Type ComponentType => _original.ComponentType;
	public override bool IsReadOnly => _original.IsReadOnly;
	public override Type PropertyType => _original.PropertyType;
}

public class FooTypeDescriptor : CustomTypeDescriptor
{
	ICustomTypeDescriptor _original;
	public FooTypeDescriptor(ICustomTypeDescriptor originalDescriptor) : base(originalDescriptor) 
		=> _original = originalDescriptor;
	
	public override PropertyDescriptorCollection GetProperties()
	{
		return this.GetProperties(new Attribute[] { });
	}
	public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
	{
		FooPropDescriptor[] properties = base
			.GetProperties(attributes)
			.Cast<PropertyDescriptor>()
			.Select(p => new FooPropDescriptor(p))
			.ToArray();
		return new PropertyDescriptorCollection(properties);
	}
}

public class FooTypeDescriptionProvider : TypeDescriptionProvider
{
	public FooTypeDescriptionProvider() : base(TypeDescriptor.GetProvider(typeof(object))) { }

	public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
		=> new FooTypeDescriptor(base.GetTypeDescriptor(objectType, instance));
}