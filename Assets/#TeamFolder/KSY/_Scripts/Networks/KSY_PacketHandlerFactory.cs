using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace KSY.Networks
{
    public class KSY_PacketHandlerFactory : MonoBehaviour
    {
        public static class Builder
        {
            public static KSY_PacketHandlerFactory Build(Assembly[] assemblies, KSY_DIContainer diContainer)
            {
                KSY_PacketHandlerFactory packetHandlerFactory = new KSY_PacketHandlerFactory
                {
                    factories = new Dictionary<Type, Func<KSY_DIContainer, KSY_IPacketHandlerBase>>(),
                    diContainer = diContainer
                };
                //IsAssignableFrom : 형변환이 가능한지 확인하는 메서드 (상속 관계, 인터페이스 구현 여부 등 확인)
                //IsDefined L 특정 커스텀 특성(Attribute)이 적용되어 있는지 여부를 확인할 때 사용하는 메서드
                Type[] array = (from t in assemblies.SelectMany((Assembly a)=>a.GetTypes())
                                where typeof(KSY_IPacketHandlerBase).IsAssignableFrom(t)
                                where t.IsDefined(typeof(KSY_PacketAttribute), inherit: false)
                                where !t.IsAbstract && !t.IsInterface
                                select t).ToArray();
                foreach (Type type in array)
                {
                    KSY_PacketHandlerAttribute customAttribute = type.GetCustomAttribute<KSY_PacketHandlerAttribute>();
                    if (customAttribute != null)
                    {
                        Type packetType = customAttribute.PacketType;
                        if(!(packetType == null) && packetType.IsDefined(typeof(KSY_PacketAttribute), inherit: false) && typeof(KSY_IPacket).IsAssignableFrom(packetType))
                        {
                            packetHandlerFactory.factories[packetType] = CreatePacketHandlerFactory(type, diContainer);
                        }
                    }
                }

                return packetHandlerFactory;
            }

            private static Func<KSY_DIContainer, KSY_IPacketHandlerBase> CreatePacketHandlerFactory(Type packetHandlerType, KSY_DIContainer diContainer)
            {
                //ConstructorInfo : 생성자에 대한 특성을 검색하고 생성자의 메타데이터에 액세스 할 수 있게 해주는 도구.
                ConstructorInfo constructorInfo = SelectConstructor(packetHandlerType, diContainer);
                if(constructorInfo == null)
                    //InvalidOperationException : 객체의 현재 상태가 호출된 메서드를 수행하기에 적절하지 않을 때 발생하는 예외.
                    throw new InvalidOperationException("No constructor matching the criteria exists for " + packetHandlerType.FullName + ".");

                //ParameterExpression : 식 트리 안에서의 매개변수
                //Expression.Parameter : 식 트리내에서 사용할 매개변수 노드를 생성하는 메서드
                ParameterExpression diContainerParam = Expression.Parameter(typeof(KSY_DIContainer), "diContainer");
                //MethodCallExpression : 식 트리내에서 메서드의 호출을 나타내는 노드
                MethodCallExpression[] array = constructorInfo.GetParameters().Select(delegate (ParameterInfo parameterInfo)
                {
                    //KSY_DIContainer에서 GetInstance라는 인스턴스 public void 메서드를 찾아서 MethodInfo 클래스에 넣어주고 있다.
                    MethodInfo method = typeof(KSY_DIContainer).GetMethod("GetInstance", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
                    return Expression.Call(diContainerParam, method.MakeGenericMethod(parameterInfo.ParameterType));
                }).ToArray();
                Expression[] arguments = array;
                return Expression.Lambda<Func<KSY_DIContainer, KSY_IPacketHandlerBase>>(Expression.Convert(Expression.New(constructorInfo, arguments), typeof(KSY_IPacketHandlerBase)), new ParameterExpression[1] {diContainerParam}).Compile();
            }
            private static ConstructorInfo SelectConstructor(Type type, KSY_DIContainer diContainer)
            {
                //Type.GetConstructors : 현재 Type의 모든 public 생성자를 나타내는 ConstructorInfo 객체의 배열을 반환하는 메서드
                ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                
                if (constructors.Length == 0)
                    return null;

                //가능한 한 많은 정보를 주입할 수 있도록 매개변수가 가장 많은 생성자를 반환한다고 함.
                return constructors.OrderByDescending((ConstructorInfo c)=>c.GetParameters().Length).ToArray()[0];
            }
        }

        private KSY_DIContainer diContainer;
        private Dictionary<Type, Func<KSY_DIContainer, KSY_IPacketHandlerBase>> factories;

        private KSY_PacketHandlerFactory()
        {
        }

        public KSY_IPacketHandlerBase Create(Type packetType)
        {
            if (!factories.TryGetValue(packetType, out var value))
            {
                return null;
            }

            return value(diContainer);
        }
    } 
}
