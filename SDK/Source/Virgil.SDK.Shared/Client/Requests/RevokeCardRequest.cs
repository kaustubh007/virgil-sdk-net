#region Copyright (C) Virgil Security Inc.
// Copyright (C) 2015-2016 Virgil Security Inc.
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
namespace Virgil.SDK.Client.Requests
{
    using Models;
    using System;
    /// <summary>
    /// Represents an information about revoking card request.
    /// </summary>
    public abstract class RevokeCardRequest : SignedRequest
    {

        private string cardId;
        private RevocationReason reason;

        public string CardId
        {
            get { return this.cardId; }
            set
            {
                if (this.IsSnapshotTaken)
                {
                    throw new InvalidOperationException();
                }

                this.cardId = value;
            }
        }

        public RevocationReason Reason
        {
            get { return this.reason; }
            set
            {
                if (this.IsSnapshotTaken)
                {
                    throw new InvalidOperationException();
                }

                this.reason = value;
            }
        }

        protected override byte[] CreateSnapshot()
        {
            var snapshotMaster = new SnapshotMaster();
            var model = new RevokeCardSnapshotModel
            {
                CardId = this.CardId,
                Reason = Enum.GetName(typeof(RevocationReason), this.Reason)?.ToLower()
            };
            return snapshotMaster.TakeSnapshot(model);
        }

        protected override bool IsValidData()
        {
            return (this.cardId != null);
        }
    }
}