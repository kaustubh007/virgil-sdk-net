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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Virgil.SDK.Signer;
using Virgil.SDK.Web.Authorization;

namespace Virgil.SDK
{
    using System.Threading.Tasks;
    using Virgil.CryptoAPI;
    using Virgil.SDK.Common;
    using Virgil.SDK.Validation;
    using Virgil.SDK.Web;

    public class CardManager
    {
        private readonly ICardCrypto cardCrypto;
        private readonly ModelSigner modelSigner;
        private readonly CardClient client;
        private readonly ICardVerifier cardVerifier;
        private readonly Func<RawSignedModel, Task<RawSignedModel>> signCallBack;
        private readonly IAccessTokenProvider accessTokenProvider;

        public CardManager(CardManagerParams @params)
        {
            ValidateCardManagerParams(@params);
            this.cardCrypto = @params.CardCrypto;
            this.accessTokenProvider = @params.AccessTokenProvider;
            this.client = string.IsNullOrWhiteSpace(@params.ApiUrl)
                ? new CardClient()
                : new CardClient(@params.ApiUrl);

            this.cardVerifier = @params.Verifier;
            this.modelSigner = new ModelSigner(cardCrypto);
            this.signCallBack = @params.SignCallBack;
        }

        private static void ValidateCardManagerParams(CardManagerParams @params)
        {
            if (@params.CardCrypto == null)
            {
                throw new ArgumentException($"{nameof(@params.CardCrypto)} property is mandatory");
            }

            if (@params.AccessTokenProvider == null)
            {
                throw new ArgumentException($"{nameof(@params.AccessTokenProvider)} property is mandatory");
            }
        }

        /// <summary>
        /// Gets the card by specified ID.
        /// </summary>
        /// <param name="cardId">The card ID to be found.</param>
        /// <returns>The instance of found <see cref="Card"/>.</returns>
        public async Task<Card> GetCardAsync(string cardId)
        {
            var tokenContext = new TokenContext(){ Operation = "get" };
            var (rawCard, isOutdated) = (Tuple<RawSignedModel, bool>)await TryExecute(
                async () =>
                {
                    return await this.client.GetCardAsync(cardId,
                        (await this.accessTokenProvider.GetTokenAsync(tokenContext)).ToString());
                }, tokenContext);
            var card = CardUtils.Parse(this.cardCrypto, (RawSignedModel)rawCard, isOutdated);

            this.ValidateCards(new[] { card });

            return card;
        }

        /// <summary>
        /// Searches for cards by specified identity.
        /// </summary>
        /// <param name="identity">The identity to be found.</param>
        public async Task<IList<Card>> SearchCardsAsync(string identity)
        {
            var tokenContext = new TokenContext() { Operation = "search" };

            var rawCards = await TryExecute(
                async () =>
                {
                    return await client.SearchCardsAsync(identity,
                        (await accessTokenProvider.GetTokenAsync(tokenContext)).ToString()
                    );
                }, tokenContext);

            var cards = CardUtils.Parse(this.cardCrypto, (IEnumerable<RawSignedModel>)rawCards).ToArray();
            this.ValidateCards(cards);
            return CardUtils.LinkedCardLists(cards);
        }

        /// <summary>
        /// Publish a new Card using specified <see cref="CardParams"/>.
        /// </summary>
        /// <param name="cardParams">The instance of <see cref="CardParams"/> class.
        /// It should has mandatory parameters: public key, private key, identity
        /// <returns>The instance of newly created <see cref="Card"/> class.</returns>
        public async Task<Card> PublishCardAsync(CardParams cardParams)
        {
            ValidateCardParams(cardParams);

            var tokenContext = new TokenContext() {
                Operation = "publish",
                Identity = cardParams.Identity};

            var token = await this.accessTokenProvider.GetTokenAsync(tokenContext);
            var rawSignedModel = GenerateRawCard(new CardParams()
            {
                Identity = token.Identity(),
                PrivateKey = cardParams.PrivateKey,
                PublicKey = cardParams.PublicKey,
                PreviousCardId = cardParams.PreviousCardId,
                ExtraFields = cardParams.ExtraFields
            });

            return await PublishRawSignedModel(rawSignedModel, tokenContext, token);
        }

        private async Task<Card> PublishRawSignedModel(RawSignedModel rawSignedModel, 
            TokenContext context, 
            IAccessToken token)
        {
            if (this.signCallBack != null)
            {
                rawSignedModel = await this.signCallBack.Invoke(rawSignedModel);
            }

            var publishedModel = await TryExecute(
                async () =>
            {
                var rawCard = await client.PublishCardAsync(
                    rawSignedModel,
                    token.ToString()
                );
                return rawCard;
            }, context);

            var card = CardUtils.Parse(this.cardCrypto, (RawSignedModel)publishedModel);
            this.ValidateCards(new[] { card });

            return card;
        }

        private async Task<object> TryExecute(Func<Task<object>> func, TokenContext context)
        {
            object result = null;
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    result = await func.Invoke();
                    break;
                }
                catch (UnauthorizedClientException e)
                {
                    if (i == 2)
                    {
                        throw e;
                    }
                    if (!context.ForceReload)
                    {
                        context.ForceReload = true;
                    }
                    await this.accessTokenProvider.GetTokenAsync(context);
                }
            }
            return result;
        }
        /// <summary>
        /// Generates a new <see cref="RawSignedModel"/> in order to apply for a card registration. 
        /// It contains the public key for 
        /// which the card should be registered, identity information (such as a user name) and integrity 
        /// protection in form of digital self signature.
        /// </summary> 
        /// <param name="cardParams">The instance of <see cref="CardParams"/> class.
        /// It should has mandatory parameters: identity, public key, private key</param>
        /// <returns>A new instance of <see cref="RawSignedModel"/> class.</returns>
        public RawSignedModel GenerateRawCard(CardParams cardParams)
        {
            ValidateCardParams(cardParams, true);
            var model = RawSignedModel.Generate(cardCrypto, cardParams);
            modelSigner.SelfSign(model, cardParams.PrivateKey, cardParams.ExtraFields);
            return model;
        }

        private static void ValidateCardParams(CardParams cardParams, bool validateIdentity = false)
        {
            if (cardParams == null)
            {
                throw new ArgumentNullException(nameof(cardParams));
            }
            if (validateIdentity && String.IsNullOrWhiteSpace(cardParams.Identity))
            {
                throw new ArgumentException($"{cardParams.Identity} property is mandatory");
            }
            if (cardParams.PublicKey == null)
            {
                throw new ArgumentException($"{cardParams.PublicKey} property is mandatory");
            }

            if (cardParams.PrivateKey == null)
            {
                throw new ArgumentException($"{cardParams.PrivateKey} property is mandatory");
            }
        }

        public async Task<Card> PublishCardAsync(RawSignedModel rawSignedModel)
        {
            var cardContent = SnapshotUtils.ParseSnapshot<RawCardContent>(
                rawSignedModel.ContentSnapshot);
            var tokenContext = new TokenContext()
            {
                Operation = "publish",
                Identity = cardContent.Identity
            };
            var token = await this.accessTokenProvider.GetTokenAsync(tokenContext);

            return await PublishRawSignedModel(rawSignedModel, tokenContext, token);
        }

        public string ExportCardAsString(Card card)
        {
            return ExportCardAsRawCard(card).ExportAsString();
        }

        public string ExportCardAsJson(Card card)
        {
            return ExportCardAsRawCard(card).ExportAsJson();
        }

        public RawSignedModel ExportCardAsRawCard(Card card)
        {
            return RawSignedModelUtils.Parse(cardCrypto, card);
        }
        public Card ImportCardFromJson(string json)
        {
            var rawSignedModel = RawSignedModel.GenerateFromJson(json);
            return CardUtils.Parse(cardCrypto, rawSignedModel);
        }

        public Card ImportCardFromString(string str)
        {
            var rawSignedModel = RawSignedModel.GenerateFromString(str);
            return CardUtils.Parse(cardCrypto, rawSignedModel);
        }

        private void ValidateCards(IEnumerable<Card> cards)
        {
            if (this.cardVerifier == null)
            {
                return;
            }

            foreach (var card in cards)
            {
                var result = this.cardVerifier.VerifyCard(card);
                if (!result)
                {
                    throw new CardValidationException("Validation errors have been detected");
                }
            }
        }
    }
}