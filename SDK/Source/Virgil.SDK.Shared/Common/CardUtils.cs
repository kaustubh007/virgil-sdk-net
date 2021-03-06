﻿#region Copyright (C) Virgil Security Inc.
// Copyright (C) 2015-2018 Virgil Security Inc.
// 
// Lead Maintainer: Virgil Security Inc. <support@virgilsecurity.com>
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions 
// are met:
// 
//   (1) Redistributions of source code must retain the above copyright
//   notice, this list of conditions and the following disclaimer.
//   
//   (2) Redistributions in binary form must reproduce the above copyright
//   notice, this list of conditions and the following disclaimer in
//   the documentation and/or other materials provided with the
//   distribution.
//   
//   (3) Neither the name of the copyright holder nor the names of its
//   contributors may be used to endorse or promote products derived 
//   from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE AUTHOR ''AS IS'' AND ANY EXPRESS OR
// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT,
// INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
// STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
// IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
#endregion


namespace Virgil.SDK.Common
{
    using Virgil.SDK.Signer;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Virgil.CryptoAPI;
    using Virgil.SDK.Web;

    public class CardUtils
    {
        public static string GenerateCardId(ICardCrypto cardCrypto, byte[] snapshot)
        {
            ValidateGenerateCardIdParams(cardCrypto, snapshot);
            var fingerprint = cardCrypto.GenerateSHA512(snapshot);
            var id = Bytes.ToString(fingerprint.Take(32).ToArray(), StringEncoding.HEX);
            return id;
        }

        private static void ValidateGenerateCardIdParams(ICardCrypto cardCrypto, byte[] snapshot)
        {
            if (cardCrypto == null)
            {
                throw new ArgumentNullException(nameof(cardCrypto));
            }

            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }
        }
        
        /// <summary>
        /// Loads <see cref="Card"/> from the specified <see cref="RawSignedModel"/>.
        /// </summary>
        /// <param name="cardCrypto">an instance of <see cref="ICardCrypto"/>.</param>
        /// <param name="rawSignedModel">an instance of <see cref="RawSignedModel"/> to get 
        /// <see cref="Card"/> from.</param>
        /// <param name="isOutdated"></param>
        /// <returns>Loaded instance of <see cref="Card"/>.</returns>
        public static Card Parse(ICardCrypto cardCrypto, RawSignedModel rawSignedModel, bool isOutdated = false)
        {
            ValidateParams(cardCrypto, rawSignedModel);

            var rawCardContent = SnapshotUtils.ParseSnapshot<RawCardContent>(rawSignedModel.ContentSnapshot);

            var signatures = new List<CardSignature>();
            if (rawSignedModel.Signatures != null)
            {
                foreach (var s in rawSignedModel.Signatures)
                {
                    var cardSignature = new CardSignature
                    {
                        Signer = s.Signer,
                        Signature = s.Signature,
                        ExtraFields = TryParseExtraFields(s.Snapshot),
                        Snapshot = s.Snapshot
                    };
                    signatures.Add(cardSignature);
                }
            }

            return new Card(
                GenerateCardId(cardCrypto, rawSignedModel.ContentSnapshot),
                rawCardContent.Identity,
                cardCrypto.ImportPublicKey(rawCardContent.PublicKey),
                rawCardContent.Version,
                rawCardContent.CreatedAt,
                signatures,
                rawCardContent.PreviousCardId,
                rawSignedModel.ContentSnapshot,
                isOutdated
                );
        }

        private static void ValidateParams(ICardCrypto cardCrypto, RawSignedModel rawSignedModel)
        {
            if (rawSignedModel == null)
            {
                throw new ArgumentNullException(nameof(rawSignedModel));
            }

            if (cardCrypto == null)
            {
                throw new ArgumentNullException(nameof(cardCrypto));
            }
        }

        private static Dictionary<string, string> TryParseExtraFields(byte[] signatureSnapshot)
        {
            Dictionary<string, string> extraFields = null;
            if (signatureSnapshot != null)
            {
                try
                {
                    extraFields = SnapshotUtils.ParseSnapshot<Dictionary<string, string>>(signatureSnapshot);
                }
                catch (Exception)
                {
                }
            }
            return extraFields;
        }

        public static IList<Card> Parse(ICardCrypto cardCrypto, IEnumerable<RawSignedModel> requests)
        {
            if (requests == null)
            {
                throw new ArgumentNullException(nameof(requests));
            }

            return requests.Select(r => CardUtils.Parse(cardCrypto, r)).ToList();
        }

        public static IList<Card> LinkedCardLists(Card[] cards)
        {
            var unsorted = new Dictionary<string, Card>();
            foreach (var card in cards)
            {
                unsorted.Add(card.Id, card);
            }

            foreach (var card in cards)
            {
                if (card.PreviousCardId != null)
                {
                    if (unsorted[card.PreviousCardId] != null)
                    {
                        unsorted[card.PreviousCardId].IsOutdated = true;
                        card.PreviousCard = unsorted[card.PreviousCardId];
                        unsorted.Remove(card.PreviousCardId);
                    }
                }
            }
            return unsorted.Values.ToList();
        }
    }
}