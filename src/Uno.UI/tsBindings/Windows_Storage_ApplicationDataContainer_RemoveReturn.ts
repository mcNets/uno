/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
namespace Windows.Storage
{
	export class ApplicationDataContainer_RemoveReturn
	{
		/* Pack=1 */
		public Removed : boolean;
		public marshal(pData:number)
		{
			Module.setValue(pData + 0, this.Removed, "i32");
		}
	}
}
