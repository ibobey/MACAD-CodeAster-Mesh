#include "ManagedPCH.h"

#include "AIS_PointEx.h"

#using "Macad.Occt.dll" as_friend

namespace Macad
{
	namespace Occt
	{
		namespace Ext
		{
			public ref class AIS_PointEx sealed : Macad::Occt::AIS_Point
			{
			public:
				IMPLEMENT_TRANSIENT(AIS_PointEx, AIS_Point);
				
				AIS_PointEx(Macad::Occt::Geom_Point^ aComponent)
				    : Macad::Occt::AIS_Point(InitMode::Uninitialized)
				{
					NativeInstance = new ::AIS_PointEx(aComponent->NativeInstance);
				}

				void EnableBackground(double theTransparency)
				{
					NativeInstance->EnableBackground(theTransparency);
				}

				void SetBackgroundColor(Macad::Occt::Quantity_Color^ aColor)
				{
					NativeInstance->SetBackgroundColor(*aColor->NativeInstance);
				}
			};
		};
	}
}
