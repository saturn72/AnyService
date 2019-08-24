using System;
using System.Collections.Generic;

namespace AnyService.Services
{
    public sealed class ValidatorFactory
    {
        private readonly Dictionary<Type, ICrudValidator> _validatorDictionary;

        public ValidatorFactory(IEnumerable<ICrudValidator> validators)
        {
            _validatorDictionary = new Dictionary<Type, ICrudValidator>();
            foreach (var v in validators)
                _validatorDictionary[v.Type] = v;
        }
        public ICrudValidator this[Type t] => _validatorDictionary[t];
    }
}