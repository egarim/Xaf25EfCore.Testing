#region Copyright (c) 2000-2024 Developer Express Inc.
/*
{*******************************************************************}
{                                                                   }
{       Developer Express .NET Component Library                    }
{                                                                   }
{                                                                   }
{       Copyright (c) 2000-2024 Developer Express Inc.              }
{       ALL RIGHTS RESERVED                                         }
{                                                                   }
{   The entire contents of this file is protected by U.S. and       }
{   International Copyright Laws. Unauthorized reproduction,        }
{   reverse-engineering, and distribution of all or any portion of  }
{   the code contained in this file is strictly prohibited and may  }
{   result in severe civil and criminal penalties and will be       }
{   prosecuted to the maximum extent possible under the law.        }
{                                                                   }
{   RESTRICTIONS                                                    }
{                                                                   }
{   THIS SOURCE CODE AND ALL RESULTING INTERMEDIATE FILES           }
{   ARE CONFIDENTIAL AND PROPRIETARY TRADE                          }
{   SECRETS OF DEVELOPER EXPRESS INC. THE REGISTERED DEVELOPER IS   }
{   LICENSED TO DISTRIBUTE THE PRODUCT AND ALL ACCOMPANYING .NET    }
{   CONTROLS AS PART OF AN EXECUTABLE PROGRAM ONLY.                 }
{                                                                   }
{   THE SOURCE CODE CONTAINED WITHIN THIS FILE AND ALL RELATED      }
{   FILES OR ANY PORTION OF ITS CONTENTS SHALL AT NO TIME BE        }
{   COPIED, TRANSFERRED, SOLD, DISTRIBUTED, OR OTHERWISE MADE       }
{   AVAILABLE TO OTHER INDIVIDUALS WITHOUT EXPRESS WRITTEN CONSENT  }
{   AND PERMISSION FROM DEVELOPER EXPRESS INC.                      }
{                                                                   }
{   CONSULT THE END USER LICENSE AGREEMENT FOR INFORMATION ON       }
{   ADDITIONAL RESTRICTIONS.                                        }
{                                                                   }
{*******************************************************************}
*/
#endregion Copyright (c) 2000-2024 Developer Express Inc.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.Persistent.Base;
namespace DevExpress.ExpressApp.Core.Internal {
	[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
	public class CustomObjectSpaceProviderContainer : IDisposable, IDisposableExt, IObjectSpaceProviderContainer, IObjectSpaceProviderContainerExt {
		readonly List<IObjectSpaceProvider> _objectSpaceProviders = new List<IObjectSpaceProvider>();
		Boolean isObjectSpaceProviderOwner = true;
		bool isDisposed;
		public bool IsEmpty => _objectSpaceProviders.Count == 0;
		bool IObjectSpaceProviderContainerExt.IsObjectSpaceProviderOwner {
			get { return isObjectSpaceProviderOwner; }
			set { isObjectSpaceProviderOwner = value; }
		}
		IEnumerable<IObjectSpaceProvider> IObjectSpaceProviderContainer.GetObjectSpaceProviders() => _objectSpaceProviders.AsReadOnly();
		IObjectSpaceProvider IObjectSpaceProviderContainer.GetObjectSpaceProvider(Type objectType) {
			IObjectSpaceProvider resultObjectSpaceProvider = null;
			if(objectType != null) {
				foreach(IObjectSpaceProvider objectSpaceProvider in _objectSpaceProviders) {
					Type originalObjectType = objectSpaceProvider.EntityStore.GetOriginalType(objectType);
					if((originalObjectType != null) && objectSpaceProvider.EntityStore.RegisteredEntities.Contains(originalObjectType)) {
						resultObjectSpaceProvider = objectSpaceProvider;
						break;
					}
				}
			}
			return resultObjectSpaceProvider;
		}
		void IObjectSpaceProviderContainer.AddObjectSpaceProviders(IEnumerable<IObjectSpaceProvider> objectSpaceProviders) {
			foreach(IObjectSpaceProvider objectSpaceProvider in objectSpaceProviders) {
				if((objectSpaceProvider != null) && !this._objectSpaceProviders.Contains(objectSpaceProvider)) {
					this._objectSpaceProviders.Add(objectSpaceProvider);
				}
			}
		}
		void IObjectSpaceProviderContainer.AddObjectSpaceProvider(IObjectSpaceProvider objectSpaceProvider) {
			_objectSpaceProviders.Add(objectSpaceProvider);
		}
		void IDisposable.Dispose() {
			SafeExecutor executor = new SafeExecutor(this);
			((IObjectSpaceProviderContainerExt)this).Dispose(executor);
			executor.ThrowExceptionIfAny();
		}
		void IObjectSpaceProviderContainerExt.Dispose(SafeExecutor executor) {
			isDisposed = true;
			Clear(executor);
		}
		void IObjectSpaceProviderContainer.Clear() {
			SafeExecutor executor = new SafeExecutor(this);
			Clear(executor);
			executor.ThrowExceptionIfAny();
		}
		void Clear(SafeExecutor executor) {
			if(!IsEmpty) {
				if(isObjectSpaceProviderOwner) {
					foreach(IObjectSpaceProvider objectSpaceProvider in _objectSpaceProviders) {
						if(objectSpaceProvider is IDisposable) {
							executor.Dispose((IDisposable)objectSpaceProvider);
						}
					}
				}
				_objectSpaceProviders.Clear();
			}
		}
		bool IDisposableExt.IsDisposed => isDisposed;
	}
}
