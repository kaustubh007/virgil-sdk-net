﻿namespace Virgil.SDK.Keys.Domain
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class RecipientsCards : List<RecipientCard>
    {
        private RecipientsCards()
        {
        }

        public static RecipientsCards Search(string identityValue, params IdentityType[] identityTypes)
        {
            throw new NotImplementedException();
        }


        public string EncryptAndSign(string sdadasd, PersonalCard newCard)
        {
            throw new NotImplementedException();
        }
    }
}