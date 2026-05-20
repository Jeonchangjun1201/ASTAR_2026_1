using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace KSY.Networks
{
    public class PacketHandlerFactory 
    {
        private PacketHandlerFactory()
        {
        }

        private DIContainer diContainer;
        private Dictionary<Type, Func<DIContainer, IPacketHandlerBase>> factories;
        
        public static class Builder
        {
            public static PacketHandlerFactory Build(Assembly[] assemblies, DIContainer diContainer)
            {
                PacketHandlerFactory packetHandlerFactory = new PacketHandlerFactory
                {
                    factories = new Dictionary<Type, Func<DIContainer, IPacketHandlerBase>>(),
                    diContainer = diContainer
                };
                //IsAssignableFrom : 형변환이 가능한지 확인하는 메서드 (상속 관계, 인터페이스 구현 여부 등 확인)
                //IsDefined : 특정 Attribute가 적용되어 있는지 여부를 확인할 때 사용하는 메서드
                Type[] array = (from t in assemblies.SelectMany((Assembly a)=>a.GetTypes())
                                where typeof(IPacketHandlerBase).IsAssignableFrom(t)
                                where t.IsDefined(typeof(PacketAttribute), inherit: false)
                                where !t.IsAbstract && !t.IsInterface
                                select t).ToArray();
                foreach (Type type in array)
                {
                    PacketHandlerAttribute customAttribute = type.GetCustomAttribute<PacketHandlerAttribute>();
                    if (customAttribute != null)
                    {
                        Type packetType = customAttribute.PacketType;
                        if(!(packetType == null) && packetType.IsDefined(typeof(PacketAttribute), inherit: false) && typeof(IPacket).IsAssignableFrom(packetType))
                        {
                            packetHandlerFactory.factories[packetType] = CreatePacketHandlerFactory(type, diContainer);
                        }
                    }
                }

                return packetHandlerFactory;
            }

            private static Func<DIContainer, IPacketHandlerBase> CreatePacketHandlerFactory(Type packetHandlerType, DIContainer diContainer)
            {
                ConstructorInfo pktHandlerConstructor = SelectConstructor(packetHandlerType, diContainer);
                if(pktHandlerConstructor == null)
                    //InvalidOperationException : 객체의 현재 상태가 호출된 메서드를 수행하기에 적절하지 않을 때 발생하는 예외.
                    throw new InvalidOperationException("No constructor matching the criteria exists for " + packetHandlerType.FullName + ".");

                ParameterExpression diContainerParam = Expression.Parameter(typeof(DIContainer), "diContainer");
                //ConstructorInfo로 구한 생성자 정보에서 각 매개변수에 대한 정보를 구한 다음에,
                //매개변수의 Instance를 구하기 위해서 DIContainer.GetInstnace 제네릭 메서드들로 변환시킨다.
                MethodCallExpression[] array = pktHandlerConstructor.GetParameters().Select(delegate (ParameterInfo parameterInfo)
                {
                    MethodInfo method = typeof(DIContainer).GetMethod("GetInstance", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
                    //Expresion.Call : 첫 번째 인자는 메서드를 호출할 객체를 넣고 두 번째 인자는 호출할 메서드에 대한 정보를 넘긴다.
                    return Expression.Call(diContainerParam, method.MakeGenericMethod(parameterInfo.ParameterType));
                }).ToArray();
                //각 매개변수를 생성하는 GetInstance<T> 호출 노드들이 담긴다.
                Expression[] arguments = array;
                
                //Expression.New 메서드에 생성자 정보와 매개변수 인스턴스를 반환하는 메서드들을 인수로 넣고 PacketHandler를 만든 다음에
                //IPacketHandlerBase로 형변환 시키는 로직을 담고 있다.
                UnaryExpression makePacketHandler_Expression = Expression.Convert(
                    Expression.New(pktHandlerConstructor, arguments), 
                    typeof(IPacketHandlerBase));
                ParameterExpression[] diContainer_Param = new ParameterExpression[1] {diContainerParam};
                Func<DIContainer, IPacketHandlerBase> packetHandlerFactory = Expression.Lambda<Func<DIContainer, IPacketHandlerBase>>
                (makePacketHandler_Expression, 
                diContainer_Param).
                Compile();

                return packetHandlerFactory;
            }
            private static ConstructorInfo SelectConstructor(Type type, DIContainer diContainer)
            {
                //Type.GetConstructors : 현재 Type의 모든 public 생성자를 나타내는 ConstructorInfo 객체의 배열을 반환하는 메서드
                ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
                
                if (constructors.Length == 0)
                    return null;

                //가능한 한 많은 정보를 주입할 수 있도록 매개변수가 가장 많은 생성자를 반환한다고 함.
                return constructors.OrderByDescending((ConstructorInfo c)=>c.GetParameters().Length).ToArray()[0];
            }
        }

        public IPacketHandlerBase Create(Type packetType)
        {
            if (!factories.TryGetValue(packetType, out var value))
            {
                return null;
            }

            return value(diContainer);
        }
    } 
}
