using RC.DataPress.Metamodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace RC.DataPress.Emit
{
    public static class ObjectFactory<T>
    {
        public static Func<T> DefaultCtorFactory = EmitDefaultCtorFactory();

        private static Func<T> EmitDefaultCtorFactory()
        {
            var type = typeof(T);

            var method = new DynamicMethod("$DefaultCtorFactory_" + type.Name, type, Type.EmptyTypes, true);
            ILGenerator gen = method.GetILGenerator();
            
            if (type.IsValueType)
            {
                LocalBuilder temp = gen.DeclareLocal(type);
                gen.Emit(OpCodes.Ldloca, temp);
                gen.Emit(OpCodes.Initobj, type);
                gen.Emit(OpCodes.Ldloc, temp);
            }
            else
            {
                gen.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
            }

            gen.Emit(OpCodes.Ret);

            return (Func<T>)method.CreateDelegate(typeof(Func<T>));
        }

        public static Func<int, T> EntityFactory(IModelManager manager)
        {
            var type = typeof(T);
            return (Func<int, T>)ObjectFactory.EntityFactory(manager, type);
        }

    }

    public static class ObjectFactory
    {
        public static Delegate EntityFactory(IModelManager manager, Type type)
        {
            var method = new DynamicMethod("_$DataPress_EntityFactory" + type.Name,
               type, new[] { typeof(int) }, true);

            var gen = method.GetILGenerator();

            EmitEntityFactory(gen, manager, manager.Entity(type));

            var resultType = typeof(Func<,>).MakeGenericType(typeof(Int32), type);
            return method.CreateDelegate(resultType);
        }

        

        public static void EmitEntityFactory(ILGenerator gen, IModelManager manager, IEntityType entity)
        {
            var derivedEntities = manager.getDerivedEntityTypes(entity.EntityClass);

            if (!entity.isAbstract)
            {
                emitDescriminatorCheck(entity);
            }

            foreach (var derivedEntitie in derivedEntities.Where(x => !x.isAbstract))
            {
                emitDescriminatorCheck(derivedEntitie);
            }

            void emitDescriminatorCheck(IEntityType entityType)
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldc_I4, entityType.Descriminator);
                var label = gen.DefineLabel();
                gen.Emit(OpCodes.Bge_Un_S, label);

                EmitHelper.EmitNewObject(gen, entityType.EntityClass);

                gen.Emit(OpCodes.Ret);
                gen.MarkLabel(label);
            }
        }
    }
}
