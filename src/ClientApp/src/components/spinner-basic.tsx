// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import React from "react"
import { IStackProps, Stack } from 'office-ui-fabric-react/lib/Stack';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { Label } from 'office-ui-fabric-react/lib/Label';

export const SpinnerBasic: React.StatelessComponent = () => {

  const rowProps: IStackProps = { horizontal: true, verticalAlign: 'center' };

  const tokens = {
    sectionStack: {
      childrenGap: 10
    },
    spinnerStack: {
      childrenGap: 20
    }
  };

  return (
    <Stack tokens={tokens.sectionStack}>
      <Stack {...rowProps} tokens={tokens.spinnerStack}>
        <Label>Loading...</Label>
        <Spinner size={SpinnerSize.medium} />
      </Stack>
    </Stack>
  );
};

export default SpinnerBasic;
  