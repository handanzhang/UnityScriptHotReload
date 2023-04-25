using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

using static AssemblyPatcher.Utils;
using OpCodes = Mono.Cecil.Cil.OpCodes;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace AssemblyPatcher
{

 
public class MethodPatcher
{
    public static TypeReference Switch(AssemblyData assemblyData ,TypeReference typeRef)
    {
        // 方案开始不支持lambda的修改
        if(IsLambdaStaticType(typeRef))
        {
            return typeRef;
        }

        // 如果能在new中找到对应的def，那就new一个新的type，完全替换，找不到就只替换泛型参数
        if(typeRef is GenericInstanceType gType)
        {
            GenericInstanceType returnType = gType;
            var list = gType.GenericArguments.ToList();

            if (gType.Resolve() != null && assemblyData.baseTypes.TryGetValue(typeRef.Resolve().FullName, out var baseDef))
            {
                var oldRef = assemblyData.ImportOldTypeRef(baseDef.definition);
                returnType = new GenericInstanceType(oldRef);
            }
            returnType.GenericArguments.Clear();
            foreach (var a in list)
            {
                returnType.GenericArguments.Add(Switch(assemblyData, a));
            }
            return returnType;
        }
        else
        {
            var def = typeRef.Resolve();
            if (def != null && assemblyData.baseTypes.TryGetValue(def.ToString(), out var baseDef))
            {
                var oldRef = assemblyData.ImportOldTypeRef(baseDef.definition);
                return oldRef ?? typeRef;
            }
            else
            {
                return typeRef;
            }
        }
    }

    //TODO 这里处理了新增情况， 以后有时间再看
    private void FieldDefineProcess(FieldDefinition fieldDef, Instruction ins, ILProcessor ilProcessor, MethodFixStatus fixStatus, Dictionary<MethodDefinition, MethodFixStatus> processed, int depth)
    {
        var fieldType = fieldDef.DeclaringType;
        if (fieldType.Module != _assemblyData.newAssDef.MainModule) 
            return;

        bool isLambda = IsLambdaStaticType(fieldType);
        if (!isLambda && _assemblyData.baseTypes.TryGetValue(fieldType.FullName, out TypeData baseTypeData))
        {
            if (baseTypeData.fields.TryGetValue(fieldDef.FullName, out FieldDefinition baseFieldDef))
            {
                var fieldRef = _assemblyData.ImportOldFieldRef(baseFieldDef);
                var newIns = Instruction.Create(ins.OpCode, fieldRef);
                ilProcessor.Replace(ins, newIns);
                fixStatus.ilFixed = true;
            }
            else
                throw new Exception($"can not find field {fieldDef.FullName} in base dll");
        }
        else
        {
            // 新定义的类型或者lambda, 可以使用新的Assembly内的定义, 但需要递归修正其中的方法
            if (_assemblyData.newTypes.TryGetValue(fieldType.ToString(), out TypeData typeData))
            {
                foreach (var kv in typeData.methods)
                {
                    PatchMethod(kv.Value.definition, processed, depth + 1);
                }
            }
        }
    }

    private void MethodProcess(MethodReference mRef)
    {
        try
        {
            mRef.DeclaringType = Switch(_assemblyData, mRef.DeclaringType);

        }catch(Exception e)
        {
            Debug.Log($"failed to set declaringType {mRef.Name}, {mRef.DeclaringType.Name},  {mRef.Module.Name}");
        }

        if (mRef.IsGenericInstance)
        {
            var gMethod = mRef as GenericInstanceMethod;
            var GAArgs = gMethod.GenericArguments;

            var list = GAArgs.ToList();
            GAArgs.Clear();
            foreach (var a in list)
            {
                GAArgs.Add(Switch(_assemblyData, a));
            }
        }

        if (mRef.Parameters.Count > 0)
        {
            var pTypes = mRef.Parameters;
            foreach (var a in pTypes)
            {
                a.ParameterType = Switch(_assemblyData, a.ParameterType);
            }
        }

        if (mRef.GenericParameters.Count > 0)
        {
            var GPTypes = mRef.GenericParameters;
            foreach (var a in GPTypes)
            {
                a.DeclaringType = Switch(_assemblyData, a);
            }
        }

        mRef.ReturnType = Switch(_assemblyData, mRef.ReturnType);
    }

        AssemblyData _assemblyData;
    public MethodPatcher(AssemblyData assemblyData)
    {
        _assemblyData = assemblyData;
    }

    public void PatchMethod(MethodDefinition definition, Dictionary<MethodDefinition, MethodFixStatus> processed, int depth)
    {
        var fixStatus = new MethodFixStatus();
        if (processed.ContainsKey(definition))
            return;
        else
            processed.Add(definition, fixStatus);

        if (!definition.HasBody)
            return;

        var sig = definition.ToString();
        if (_assemblyData.methodsNeedHook.ContainsKey(sig))
            fixStatus.needHook = true;

        // 参数和返回值由于之前已经检查过名称是否一致，因此是二进制兼容的，可以不进行检查

        var arrIns = definition.Body.Instructions.ToArray();
        var ilProcessor = definition.Body.GetILProcessor();

        for (int i = 0, imax = arrIns.Length; i < imax; i++)
        {

            Instruction ins = arrIns[i];
            /*
                * Field 有两种类型: FieldReference/FieldDefinition, 经过研究发现 FieldReference 指向了当前类型外部的定义（当前Assembly或者引用的Assembly）, 
                * 而 FieldDefinition 则是当前类型内定义的字段
                * 因此我们需要检查 FieldDefinition 和 FieldReference, 把它们都替换成原始 Assembly 内同名的 FieldReference
                * Method/Type 同理, 但 lambda 表达式不进行替换，而是递归修正函数
                */
                
            if (ins.Operand == null)
                continue;

            if (ins.Operand is TypeDefinition typeDef)
            {
                // 类型定义，直接替换吧
                if (IsLambdaStaticType(typeDef))
                {
                    continue;
                }
                else
                {
                    if (_assemblyData.baseTypes.TryGetValue(typeDef.ToString(), out var baseDef))
                    {
                        var oldRef = _assemblyData.ImportOldTypeRef(baseDef.definition);
                        ilProcessor.Replace(ins, Instruction.Create(ins.OpCode, oldRef));
                        fixStatus.ilFixed = true;
                    }
                }

            }
            else if (ins.Operand is TypeReference)
            {
                var operand = ins.Operand as TypeReference;
                var typeRef = Switch(_assemblyData, operand);
                ilProcessor.Replace(ins, Instruction.Create(ins.OpCode, typeRef));
                fixStatus.ilFixed = true;
            }

            else if (ins.Operand is FieldDefinition fDef)
            {
                if(!IsLambdaStaticType(fDef.DeclaringType))
                {
                    if (_assemblyData.baseTypes.TryGetValue(fDef.DeclaringType.FullName, out TypeData baseData))
                    {
                        if (baseData.fields.TryGetValue(fDef.FullName, out FieldDefinition baseField))
                        {
                            var fieldRef = _assemblyData.ImportOldFieldRef(baseField);
                            ilProcessor.Replace(ins, Instruction.Create(ins.OpCode, fieldRef));
                            fixStatus.ilFixed = true;
                        }
                    }
                }
            }

            else if (ins.Operand is FieldReference fRef)
            {
                var def = fRef.Resolve();
                if (def != null && _assemblyData.baseTypes.TryGetValue(def.DeclaringType.FullName, out var baseDef))
                {
                    // 新增字段，不处理
                    if (baseDef.fields.TryGetValue(def.ToString(), out FieldDefinition baseField))
                    {
                        var oldRef = _assemblyData.ImportOldFieldRef(baseField);
                        oldRef.DeclaringType = Switch(_assemblyData, fRef.DeclaringType);
                        ilProcessor.Replace(ins, Instruction.Create(ins.OpCode, oldRef));
                    }
                }

                else
                {
                    fRef.DeclaringType = Switch(_assemblyData, fRef.DeclaringType);
                }
            }

            else if (ins.Operand is MethodDefinition mDef)
            {

                if (IsLambdaMethod(mDef))
                {
                    //TODO lambda 或者新增
                    //PatchMethod(methodDef, processed, depth + 1); // TODO 当object由非hook代码创建时调用新添加的虚方法可能有问题
                    continue;
                }
                if ( _assemblyData.allBaseMethods.TryGetValue(mDef.ToString(), out MethodData baseMethodDef))
                {
                    var reference = _assemblyData.ImportOldMethodRef(baseMethodDef.definition);
                    var newIns = Instruction.Create(ins.OpCode, reference);
                    ilProcessor.Replace(ins, newIns);
                    fixStatus.ilFixed = true;
                }
                if(_assemblyData.allNewMethods.TryGetValue(mDef.ToString(), out MethodData newMethodDef))
                {
                    //遇到新方法，对实例的访问，改成static形式
                    ilProcessor.Replace(ins, Instruction.Create(OpCodes.Call, mDef));
                    fixStatus.ilFixed = true;
                }
            }
                
            else if (ins.Operand is MethodReference mRef)
            {

                // reference 不存在新增。即时存在引用了新增的方法，也是先跳转老的，然后hook到新方法
                MethodProcess(mRef);
                fixStatus.ilFixed = true;
            }

            else
            {
                var t = ins.Operand?.GetType();
            }

        } // for

        // 即使没有修改任何IL，也需要刷新pdb, 因此在头部给它加个nop
        if (!fixStatus.ilFixed)
            ilProcessor.InsertBefore(ilProcessor.Body.Instructions[0], Instruction.Create(OpCodes.Nop));
    }

    /// <summary>
    /// 获取/生成 Base Assembly 内的类型定义
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    GenericInstanceType GetBaseInstanceType(GenericInstanceType t)
    {
        /*
         * 1. 找到原始dll中同名的泛型类型
         * 2. 使用原始dll中的真实类型填充泛型参数（可能需要递归）
         */
        var gA = t.GenericArguments.ToArray();
        var gP = t.GenericParameters.ToArray();
        return null;
    }

    /// <summary>
    /// 获取/生成 Base Assembly 内的方法定义
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    GenericInstanceMethod GetBaseInstanceMethod(GenericInstanceMethod m)
    {
        var gA = m.GenericArguments.ToArray();
        var gP = m.GenericParameters.ToArray();
        var eleMethod = m.ElementMethod;
        var methods = (m.DeclaringType as TypeDefinition)?.Methods.ToArray();
        return null;
    }

    /// <summary>
    /// 获取 Base Assembly 内的类型引用
    /// </summary>
    /// <param name="typeRef"></param>
    /// <returns></returns>
    TypeReference GetBaseTypeRef(TypeReference typeRef)
    {
        if (typeRef is GenericInstanceType genericTypeRef)
            return GetBaseInstanceType(genericTypeRef);

        return null;
    }

    /// <summary>
    /// 获取 Base Assembly 内的字段引用
    /// </summary>
    /// <param name="fieldRef"></param>
    /// <returns></returns>
    FieldReference GetBaseFieldRef(FieldReference fieldRef)
    {
        return null;
    }

    /// <summary>
    /// 获取 Base Assembly 内的方法引用
    /// </summary>
    /// <param name="methodRef"></param>
    /// <returns></returns>
    MethodReference GetBaseMethodRef(MethodReference methodRef)
    {
        if (methodRef is GenericInstanceMethod genericMethodRef)
            return GetBaseInstanceMethod(genericMethodRef);

        return null;
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsDefInCurrentAssembly(TypeReference typeRef)
    {
        return typeRef.Scope == _assemblyData.newAssDef.MainModule;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsDefInCurrentAssembly(FieldReference fieldRef)
    {
        return fieldRef.DeclaringType.Scope == _assemblyData.newAssDef.MainModule;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsDefInCurrentAssembly(MethodReference methodRef)
    {
        return methodRef.DeclaringType.Scope == _assemblyData.newAssDef.MainModule;
    }

}

}
